using System.Diagnostics;
using ATL;
using ATL.Logging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.Types;

public class Mp3FileObject : ObservableObject {
    #region The TagLib#-Music file(s)

    public Track  Mp3File               { get; set; }
    public Track Mp3FileInInitialState { get; set; }

    #endregion

    #region Properties

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

    // Legacy Properties (ignorieren)
    private List<string> _artists;

    public List<string> Artists {
        get => _artists;
        set {
            _artists = value;
            OnPropertyChanged();
        }
    }

    private string _songTitle;

    public string SongTitle {
        get => _songTitle;
        set {
            _songTitle = value;
            OnPropertyChanged();
        }
    }

    private string _albumTitle;

    public string AlbumTitle {
        get => _albumTitle;
        set {
            _albumTitle = value;
            OnPropertyChanged();
        }
    }

    // WICHTIG: ID3 Properties - diese verwenden wir
    private string _id3Title;

    public string Id3Title {
        get => _id3Title;
        set {
            _id3Title     = value;
            // if (File?.Tag != null) 
            Mp3File.Title = value;
            OnPropertyChanged();
        }
    }

    private string[] _id3Artists;

    public string[] Id3Artists {
        get => _id3Artists;
        set {
            _id3Artists            = value;
            Mp3File.InvolvedPeople = string.Join(";", value);
            OnPropertyChanged();
        }
    }

    private string _id3Album;

    public string Id3Album {
        get => _id3Album;
        set {
            _id3Album     = value;
            // if (File?.Tag != null) 
            Mp3File.Album = value;
            OnPropertyChanged();
        }
    }

    private int? _id3ReleaseYear;

    public int? Id3ReleaseYear {
        get => _id3ReleaseYear;
        set {
            _id3ReleaseYear = value;
            // if (File != null) 
            Mp3File.Year    = value;
            OnPropertyChanged();
        }
    }

    private string _id3Comment;

    public string Id3Comment {
        get => _id3Comment;
        set {
            _id3Comment     = value;
            // if (File.Comment != null) 
            Mp3File.Comment = value;
            OnPropertyChanged();
        }
    }

    private bool _isSelected;

    public bool IsSelected {
        get => _isSelected;
        set {
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    private string _id3Genre;

    public string Id3Genre {
        get => _id3Genre;
        set {
            _id3Genre = value;
            OnPropertyChanged();
        }
    }

    public string[] Id3GenresAsArray {
        get {
            string[] tmp    = _id3Genre.Split(';');
            string[] result = new string[tmp.Length];
            for (int i = 0; i < tmp.Length; i++) {
                var trim = tmp[i].Trim();
                result[i] = trim;
            }

            return result;
        }
        set {
            _id3Genre = string.Empty;
            // if (value != null) {
            foreach (string val in value) {
                _id3Genre += val + ";";
            }
            // }

            // if (File?.Tag != null) 
            Mp3File.Genre = _id3Genre;
            OnPropertyChanged();
        }
    }

    public string Id3GenresAsString {
        get => string.Join(";", _id3Genre);
        set {
            if (string.IsNullOrEmpty(value)) {
                Id3GenresAsArray = [];
            }
            else {
                Id3GenresAsArray = value.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))
                    .ToArray();
            }

            OnPropertyChanged();
        }
    }

    public int Bitrate { get; set; }

    #endregion

    #region Constructor

    public Mp3FileObject(string path) {
        Id3Genre = string.Empty;
        _artists = [];

        try {
            Mp3File = new Track(path);
        }
        catch (Exception ex) {
            Mp3File = null;
            Debug.WriteLine($"Fehler beim Laden der Datei {path}: {ex.Message}");
            return;
        }

        if (Mp3File != null) {
            int startIndex = path.LastIndexOf(Path.DirectorySeparatorChar);
            FileName                = path[(startIndex + 1)..];
            ContainingDirectoryName = path[..startIndex];
            FileType                = Mp3File.AudioFormat;
            Bitrate                 = Mp3File.Bitrate;

            // ID3-Tags direkt laden
            LoadID3TagsFromFile();
        }

        WasManipulated        = false;
        Mp3FileInInitialState = Mp3File;
    }

    #endregion

    #region Private Methods

    private void LoadID3TagsFromFile() {
        if (Mp3File == null) return;

        // Direkte Zuweisung ohne rekursive Setter
        _id3Title       = Mp3File.Title;
        _id3Album       = Mp3File.Album;
        _id3ReleaseYear = Mp3File.OriginalReleaseYear;
        _id3Comment     = Mp3File.Comment;

        // Genres
        _id3Genre = Mp3File.Genre;


        // Künstler: Direkt parsen ohne rekursive Setter
        ParseAndSetArtistsFromTag();

        // PropertyChanged events manuell auslösen
        OnPropertyChanged(nameof(Id3Title));
        OnPropertyChanged(nameof(Id3Album));
        OnPropertyChanged(nameof(Id3ReleaseYear));
        OnPropertyChanged(nameof(Id3Comment));
        OnPropertyChanged(nameof(Id3Genre));
        OnPropertyChanged(nameof(Id3GenresAsArray));
        OnPropertyChanged(nameof(Id3GenresAsString));
    }

    private void ParseAndSetArtistsFromTag() {
        if (Mp3File?.InvolvedPeople == null || Mp3File.InvolvedPeople.Split(';').Length == 0) {
            _id3Artists = [];
            _artists    = [];
            OnPropertyChanged(nameof(Id3Artists));
            OnPropertyChanged(nameof(Artists));
            return;
        }

        var allArtists = new List<string>();

        foreach (var performer in Mp3File.InvolvedPeople.Split(';')) {
            // Track.Artist vs Track.InvolvedPeople? - Welche Properties sollte man nutzen?
            if (string.IsNullOrWhiteSpace(performer))
                continue;

            // Splitte nach allen möglichen Trennzeichen
            var splitArtists = performer.Split([',', ';', '/', '&', '+'],
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var artist in splitArtists) {
                var trimmed = artist.Trim();

                // Entferne "feat.", "ft.", etc.
                trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s*(feat\.|ft\.|featuring|with)\s*",
                    "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (!string.IsNullOrWhiteSpace(trimmed)) {
                    allArtists.Add(trimmed);
                }
            }
        }

        // Doppelte entfernen
        allArtists = allArtists.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Direkt zuweisen
        _id3Artists = allArtists.ToArray();
        _artists    = allArtists;

        // Events auslösen
        OnPropertyChanged(nameof(Id3Artists));
        OnPropertyChanged(nameof(Artists));

        Debug.WriteLine($"Geparste Künstler für {FileName}: {string.Join(", ", allArtists)}");
    }

    #endregion

    #region Public Methods

    public void SaveTags() {
        try {
            if (Mp3File != null) {
                // Sicherstellen, dass alle ID3-Properties im Tag sind
                // if (File.Tag != null) {
                Mp3File.Title          = _id3Title;
                Mp3File.Artist         = _id3Artists[0];
                Mp3File.InvolvedPeople = string.Join(';', _id3Artists);
                Mp3File.Album          = _id3Album;
                Mp3File.Year           = _id3ReleaseYear;
                Mp3File.Comment        = _id3Comment;
                Mp3File.Genre          = _id3Genre;
                // }

                Mp3File.Save();
                WasManipulated = false;
                Debug.WriteLine($"Tags gespeichert: {FileName}");
            }
        }
        catch (Exception ex) {
            Debug.WriteLine($"Fehler beim Speichern von {FileName}: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Experimental Metadata extraction

    public string[] GetMoods(Track file) {
        // .mp3
        var fileType = file.GetType();
        var moodVal  = string.Empty;

        moodVal = file.AdditionalFields["TMOO"];
        moodVal = file.AdditionalFields["mood"];


        try { }
        catch (Exception ex) {
            ATL.Logging.Log Logger = new Log();
            Logger.Error($"Fehler beim Lesen der Moods: {ex.Message}");
        }

        return Array.Empty<string>();
    }

    public string[] GetGenres(Track file) {
        return Array.Empty<string>();
    }

    #endregion
}