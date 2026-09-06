using System.Collections.ObjectModel;
using System.Diagnostics;
using ATL;
using ATL.Logging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.Types;

public class Mp3FileObject : ObservableObject {
    /// <summary>
    /// Constructor loads the music files, creates ATL.Track-Objects and populates the MP3FileObject with their values. 
    /// </summary>
    /// <param name="path"></param>
    public Mp3FileObject(string path) {
        try {
            Mp3File               = new Track(path);
            Mp3FileInInitialState = new Track(path); // Duplicate Of Original Values One Can Reset To...
        } catch (Exception ex) {
            Mp3File = null;
            Debug.WriteLine($"Fehler beim Laden der Datei {path}: {ex.Message}");
            return;
        }

        int startIndex = path.LastIndexOf(Path.DirectorySeparatorChar);
        FileName                = path[(startIndex + 1)..];
        ContainingDirectoryName = path[..startIndex];
        FileType                = Mp3File.AudioFormat;
        Bitrate                 = Mp3File.Bitrate;

        // ID3-Tags direkt laden
        LoadID3TagsFromFile();


        WasManipulated = false;
    }


    #region The TagLib#-Music file(s)

    public Track Mp3File               { get; set; }
    public Track Mp3FileInInitialState { get; set; }

    #endregion


    #region Properties

    private bool _isSelected;

    public bool IsSelected {
        get => _isSelected;
        set {
            _isSelected = value;
            OnPropertyChanged();
        }
    }


    private bool _wasManipulated;

    public bool WasManipulated {
        get => _wasManipulated;
        set {
            _wasManipulated = value;
            OnPropertyChanged();
        }
    }


    private string _fileName;

    public string FileName {
        get => _fileName;
        set {
            _fileName = value;
            OnPropertyChanged();
        }
    }


    private string _containingDirectoryName;

    public string ContainingDirectoryName {
        get => _containingDirectoryName;
        set {
            _containingDirectoryName = value;
            OnPropertyChanged();
        }
    }

    public string FullPath => Path.Combine(ContainingDirectoryName, FileName);


    private AudioFormat _fileType;

    public AudioFormat FileType {
        get => _fileType;
        set {
            _fileType = value;
            OnPropertyChanged();
        }
    }

    public int Bitrate { get; }

    #endregion


    #region ID3 Properties

    private string _id3Title;

    public string Id3Title {
        get => _id3Title;
        set {
            if (ReferenceEquals(_id3Title, value)) return;
            _id3Title     = value;
            Mp3File.Title = value;
            Mp3File.Save();
            OnPropertyChanged();
        }
    }


    private string _id3Album;

    public string Id3Album {
        get => _id3Album;
        set {
            if (ReferenceEquals(_id3Album, value)) return;
            _id3Album     = value;
            Mp3File.Album = value;
            Mp3File.Save();
            OnPropertyChanged();
        }
    }


    private int? _id3ReleaseYear;

    public int? Id3ReleaseYear {
        get => _id3ReleaseYear;
        set {
            _id3ReleaseYear             = value;
            Mp3File.OriginalReleaseYear = value;
            Mp3File.Save();
            OnPropertyChanged();
        }
    }


    private string _id3Comment;

    public string Id3Comment {
        get => _id3Comment;
        set {
            if (ReferenceEquals(_id3Comment, value)) return;
            _id3Comment     = value ?? string.Empty;
            Mp3File.Comment = value;
            Mp3File.Save();
            OnPropertyChanged();
        }
    }


    private ObservableCollection<string> _id3Artists;

    public ObservableCollection<string> Id3ArtistCollection {
        get => _id3Artists;
        set {
            if (ReferenceEquals(_id3Artists, value)) return;
            _id3Artists    = value ?? [];
            Mp3File.Artist = JoinTags(Id3ArtistCollection);
            Mp3File.Save();
            OnPropertyChanged();
        }
    }


    private ObservableCollection<string> _id3Genres;

    public ObservableCollection<string> Id3GenreCollection {
        get => _id3Genres;
        set {
            if (ReferenceEquals(_id3Genres, value)) return;

            _id3Genres    = value ?? [];
            Mp3File.Genre = JoinTags(value);
            Mp3File.Save();
            OnPropertyChanged();
        }
    }


    private ObservableCollection<string> _id3Moods;

    public ObservableCollection<string> Id3MoodCollection {
        get => _id3Moods;
        set {
            if (ReferenceEquals(_id3Moods, value)) return;

            _id3Moods                        = value ?? [];
            Mp3File.AdditionalFields["MOOD"] = JoinTags(value);
            Mp3File.AdditionalFields["TMOO"] = JoinTags(value);
            Mp3File.Save();
            OnPropertyChanged();
        }
    }

    #endregion


    private void LoadID3TagsFromFile() {
        // Direkte Zuweisung ohne rekursive Setter
        _id3Title       = Mp3File.Title;
        _id3Album       = Mp3File.Album;
        _id3ReleaseYear = Mp3File.OriginalReleaseYear;
        _id3Comment     = Mp3File.Comment;
        _id3Genres      = SplitTags(Mp3File.Genre);
        _id3Moods       = GetMoods(Mp3File);


        // Künstler: Direkt parsen ohne rekursive Setter
        ParseAndSetArtistsFromTag();

        // PropertyChanged events manuell auslösen
        OnPropertyChanged(nameof(Id3Title));
        OnPropertyChanged(nameof(Id3Album));
        OnPropertyChanged(nameof(Id3ReleaseYear));
        OnPropertyChanged(nameof(Id3Comment));
        OnPropertyChanged(nameof(Id3GenreCollection));
        OnPropertyChanged(nameof(Id3MoodCollection));
    }


    public async Task SaveTags() {
        try {
            // Sicherstellen, dass alle ID3-Properties im Tag sind
            Mp3File.Title               = _id3Title;
            Mp3File.Album               = _id3Album;
            Mp3File.Artist              = string.Join(';', _id3Artists);
            Mp3File.OriginalReleaseYear = _id3ReleaseYear;
            Mp3File.Comment             = _id3Comment;
            Mp3File.Genre               = JoinTags(_id3Genres);

            Mp3File.Save();
            WasManipulated = false;
            Debug.WriteLine($"Tags gespeichert: {FileName}");
        } catch (Exception ex) {
            Debug.WriteLine($"Fehler beim Speichern von {FileName}: {ex.Message}");
            await BuranMessageBox.Show($"Fehler beim Speichern von {FileName}: {ex.Message}");
            throw;
        }
    }

    public async Task ResetId3Tags() {
        Id3Title            = Mp3FileInInitialState.Title;
        Id3Album            = Mp3FileInInitialState.Album;
        Id3ArtistCollection = SplitTags(Mp3FileInInitialState.Artist);
        Mp3File.AlbumArtist = Mp3FileInInitialState.AlbumArtist;
        Mp3File.Year        = Mp3FileInInitialState.Year;
        Mp3File.Genre       = Mp3FileInInitialState.Genre;
        Mp3File.Comment     = Mp3FileInInitialState.Comment;


        await Mp3File.SaveAsync();
    }


    private void ParseAndSetArtistsFromTag() {
        if (string.IsNullOrEmpty(Mp3File.Artist)) {
            _id3Artists = [];
            OnPropertyChanged(nameof(Id3ArtistCollection));
            return;
        }

        ObservableCollection<string> allArtists = [];

        foreach (var performer in Mp3File.Artist.Split(';')) {
            if (string.IsNullOrWhiteSpace(performer))
                continue;

            // Splitte nach allen möglichen Trennzeichen
            var splitArtists = performer.Split([',', ';', '/', '&', '+'],
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var artist in splitArtists) {
                var trimmed = artist.Trim();

                // Entferne "feat.", "ft.", etc.
                trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s*(feat\.|ft\.|featuring|with)\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!string.IsNullOrWhiteSpace(trimmed)) {
                    allArtists.Add(trimmed);
                }
            }
        }

        // Doppelte entfernen
        allArtists = new ObservableCollection<string>(allArtists.Distinct(StringComparer.OrdinalIgnoreCase).ToList());

        // Direkt zuweisen
        _id3Artists = allArtists;

        // Events auslösen
        OnPropertyChanged(nameof(Id3ArtistCollection));

        Debug.WriteLine($"Geparste Künstler für {FileName}: {string.Join(", ", allArtists)}");
    }

    public ObservableCollection<string> GetMoods(Track file) {
        ObservableCollection<string> moodVal = [];
        try {
            file.AdditionalFields.TryGetValue("TMOO", out string? TMOOString);
            List<string> moods = [];
            if (!string.IsNullOrEmpty(TMOOString)) {
                moods = TMOOString.Split(';').ToList();
            }

            file.AdditionalFields.TryGetValue("MOOD", out string? MOODString);
            if (!string.IsNullOrEmpty(MOODString)) {
                moods.AddRange(MOODString.Split(';').ToList());
            }

            foreach (string mood in moods) {
                if (moodVal.Contains(mood)) continue;
                moodVal.Add(mood);
            }
        } catch (Exception ex) {
            ATL.Logging.Log Logger = new Log();
            Logger.Error($"Fehler beim Lesen der Moods: {ex.Message}");
            BuranMessageBox.Show($"Fehler beim Lesen der Moods: {ex.Message}").Wait();
        }

        return moodVal;
    }


    private static ObservableCollection<string> SplitTags(string? raw) {
        var result = string.IsNullOrWhiteSpace(raw) ? [] : new ObservableCollection<string>(raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());
        return result;
    }

    private static string JoinTags(IEnumerable<string>? items) => items is null ? string.Empty : string.Join(';', items.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));


    private string _GenreToAdd;

    public string GenreToAdd {
        get => _GenreToAdd;
        set {
            _GenreToAdd = value;
            OnPropertyChanged();
        }
    }

    private string _ArtistToAdd;

    public string ArtistToAdd {
        get => _ArtistToAdd;
        set {
            _ArtistToAdd = value;
            OnPropertyChanged();
        }
    }

    private string _MoodToAdd;

    public string MoodToAdd {
        get => _MoodToAdd;
        set {
            _MoodToAdd = value;
            OnPropertyChanged();
        }
    }
}