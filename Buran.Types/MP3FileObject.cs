using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ATL;
using ATL.Logging;

namespace Buran.Types;

public class Mp3FileObject : INotifyPropertyChanged {
    #region PropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region The TagLib#-Music file(s)

    public Track? TagLibFile { get; set; }
    public Track? FileInInitialState { get; set; }

    #endregion

    #region Properties

    private bool _wasManipulated;

    public bool WasManipulated {
        get => _wasManipulated;
        set {
            _wasManipulated = value;
            NotifyPropertyChanged();
        }
    }

    private string _fileName;

    public string FileName {
        get => _fileName;
        set {
            _fileName = value;
            NotifyPropertyChanged();
        }
    }

    private string _containingDirectoryName;

    public string ContainingDirectoryName {
        get => _containingDirectoryName;
        set {
            _containingDirectoryName = value;
            NotifyPropertyChanged();
        }
    }

    public string FullPath => Path.Combine(ContainingDirectoryName, FileName);

    private AudioFormat _fileType;

    public AudioFormat FileType {
        get => _fileType;
        set {
            _fileType = value;
            NotifyPropertyChanged();
        }
    }

    // Legacy Properties (ignorieren)
    private List<string> _artists;

    public List<string> Artists {
        get => _artists;
        set {
            _artists = value;
            NotifyPropertyChanged();
        }
    }

    private string _songTitle;

    public string SongTitle {
        get => _songTitle;
        set {
            _songTitle = value;
            NotifyPropertyChanged();
        }
    }

    private string _albumTitle;

    public string AlbumTitle {
        get => _albumTitle;
        set {
            _albumTitle = value;
            NotifyPropertyChanged();
        }
    }

    // WICHTIG: ID3 Properties - diese verwenden wir
    private string _id3Title;

    public string Id3Title {
        get => _id3Title;
        set {
            _id3Title = value;
            // if (File?.Tag != null) 
            TagLibFile.Title = value;
            NotifyPropertyChanged();
        }
    }

    private string[] _id3Artists;

    public string[] Id3Artists {
        get => _id3Artists;
        set {
            _id3Artists               = value;
            TagLibFile.InvolvedPeople = string.Join(";", value);
            NotifyPropertyChanged();
        }
    }

    private string _id3Album;

    public string Id3Album {
        get => _id3Album;
        set {
            _id3Album = value;
            // if (File?.Tag != null) 
            TagLibFile.Album = value;
            NotifyPropertyChanged();
        }
    }

    private int? _id3ReleaseYear;

    public int? Id3ReleaseYear {
        get => _id3ReleaseYear;
        set {
            _id3ReleaseYear = value;
            // if (File != null) 
            TagLibFile.Year = value;
            NotifyPropertyChanged();
        }
    }

    private string _id3Comment;

    public string Id3Comment {
        get => _id3Comment;
        set {
            _id3Comment = value;
            // if (File.Comment != null) 
            TagLibFile.Comment = value;
            NotifyPropertyChanged();
        }
    }

    private bool _isSelected;

    public bool IsSelected {
        get => _isSelected;
        set {
            _isSelected = value;
            NotifyPropertyChanged();
        }
    }

    private string _id3Genre;

    public string Id3Genre {
        get => _id3Genre;
        set {
            _id3Genre = value;
            NotifyPropertyChanged();
        }
    }

    public string[] Id3GenresAsArray {
        get {
            string[] tmp = _id3Genre.Split(';');
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
            TagLibFile.Genre = _id3Genre;
            NotifyPropertyChanged();
        }
    }

    public string Id3GenresAsString {
        get => string.Join(";", _id3Genre);
        set {
            if (string.IsNullOrEmpty(value)) {
                Id3GenresAsArray = [];
            } else {
                Id3GenresAsArray = value.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            }
            NotifyPropertyChanged();
        }
    }

    public int Bitrate { get; set; }

    #endregion

    #region Constructor

    public Mp3FileObject(string path) {
        Id3Genre = string.Empty;
        _artists = [];

        try {
            TagLibFile = new Track(path);
        } catch (Exception ex) {
            TagLibFile = null;
            Debug.WriteLine($"Fehler beim Laden der Datei {path}: {ex.Message}");
            return;
        }

        if (TagLibFile != null) {
            int startIndex = path.LastIndexOf('\\');
            FileName                = path[(startIndex + 1)..];
            ContainingDirectoryName = path[..startIndex];
            FileType                = TagLibFile.AudioFormat;
            Bitrate                 = TagLibFile.Bitrate;

            // ID3-Tags direkt laden
            LoadID3TagsFromFile();
        }

        WasManipulated     = false;
        FileInInitialState = TagLibFile;
    }

    #endregion

    #region Private Methods

    private void LoadID3TagsFromFile() {
        if (TagLibFile == null) return;

        // Direkte Zuweisung ohne rekursive Setter
        _id3Title       = TagLibFile.Title;
        _id3Album       = TagLibFile.Album;
        _id3ReleaseYear = TagLibFile.OriginalReleaseYear;
        _id3Comment     = TagLibFile.Comment;

        // Genres
        _id3Genre = TagLibFile.Genre;


        // Künstler: Direkt parsen ohne rekursive Setter
        ParseAndSetArtistsFromTag();

        // PropertyChanged events manuell auslösen
        NotifyPropertyChanged(nameof(Id3Title));
        NotifyPropertyChanged(nameof(Id3Album));
        NotifyPropertyChanged(nameof(Id3ReleaseYear));
        NotifyPropertyChanged(nameof(Id3Comment));
        NotifyPropertyChanged(nameof(Id3Genre));
        NotifyPropertyChanged(nameof(Id3GenresAsArray));
        NotifyPropertyChanged(nameof(Id3GenresAsString));
    }

    private void ParseAndSetArtistsFromTag() {
        if (TagLibFile?.InvolvedPeople == null || TagLibFile.InvolvedPeople.Split(';').Length == 0) {
            _id3Artists = [];
            _artists = [];
            NotifyPropertyChanged(nameof(Id3Artists));
            NotifyPropertyChanged(nameof(Artists));
            return;
        }

        var allArtists = new List<string>();

        foreach (var performer in TagLibFile.InvolvedPeople.Split(';')) {
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
        _artists = allArtists;

        // Events auslösen
        NotifyPropertyChanged(nameof(Id3Artists));
        NotifyPropertyChanged(nameof(Artists));

        Debug.WriteLine($"Geparste Künstler für {FileName}: {string.Join(", ", allArtists)}");
    }

    #endregion

    #region Public Methods

    public void SaveTags() {
        try {
            if (TagLibFile != null) {
                // Sicherstellen, dass alle ID3-Properties im Tag sind
                // if (File.Tag != null) {
                TagLibFile.Title          = _id3Title;
                TagLibFile.Artist         = _id3Artists[0];
                TagLibFile.InvolvedPeople = string.Join(';', _id3Artists);
                TagLibFile.Album          = _id3Album;
                TagLibFile.Year           = _id3ReleaseYear;
                TagLibFile.Comment        = _id3Comment;
                TagLibFile.Genre          = _id3Genre;
                // }

                TagLibFile.Save();
                WasManipulated = false;
                Debug.WriteLine($"Tags gespeichert: {FileName}");
            }
        } catch (Exception ex) {
            Debug.WriteLine($"Fehler beim Speichern von {FileName}: {ex.Message}");
            throw;
        }
    }

    #endregion

    #region Experimental Metadata extraction

    public string[] GetMoods(Track file) {
        // .mp3
        var fileType = file.GetType();
        var moodVal = string.Empty;

        moodVal = file.AdditionalFields["TMOO"];
        moodVal = file.AdditionalFields["mood"];


        try { } catch (Exception ex) {
            ATL.Logging.Log Logger = new Log();
            Logger.Error($"Fehler beim Lesen der Moods: {ex.Message}");
        }

        // TagLib.Id3v2.Tag id3 = (TagLib.Id3v2.Tag)file.GetTag(TagLib.TagTypes.Id3v2);
        // if (id3 != null) {
        //     ByteVector moodFrameId = TagLib.ByteVector.FromString("TMOO", TagLib.StringType.UTF8);
        //     TextInformationFrame TIF = TextInformationFrame.Get(id3, moodFrameId, true);
        //     if (TIF.Text.Count() > 0) {
        //         return TIF.Text;
        //     }
        // }
        //
        // // .wma / .wmv
        // TagLib.Asf.Tag asf = (TagLib.Asf.Tag)file.GetTag(TagLib.TagTypes.Asf);
        // if (asf != null) {
        //     string[] value = asf.GetDescriptorStrings("WM/Mood", "Mood");
        //     if (value.Length > 0) {
        //         return value;
        //     }
        // }
        //
        // // .flac / .aiff / .wav
        // var Xiph = file.GetTag(TagLib.TagTypes.Xiph);
        // if (Xiph != null) {
        //     // Xiph Comment parsing hier implementieren
        // }
        //
        // // .mpc / .ape / .wv / .mp3
        // TagLib.Ape.Tag ape = (TagLib.Ape.Tag)file.GetTag(TagLib.TagTypes.Ape);
        // if (ape != null) {
        //     Item item = ape.GetItem("MOOD");
        //     if (item != null)
        //         return item.ToStringArray();
        // }

        return Array.Empty<string>();
    }

    public string[] GetGenres(Track file) {
        return Array.Empty<string>();
    }

    #endregion
}