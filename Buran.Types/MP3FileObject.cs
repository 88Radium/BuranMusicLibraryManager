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
            Mp3File = new Track(path);
        } catch (Exception ex) {
            Debug.WriteLine($"Failed to load file {path}: {ex.Message}");
            FileName                = Path.GetFileName(path);
            ContainingDirectoryName = Path.GetDirectoryName(path) ?? "";
            CaptureEditBaseline();
            return;
        }

        int startIndex = path.LastIndexOf(Path.DirectorySeparatorChar);
        FileName                = path[(startIndex + 1)..];
        ContainingDirectoryName = path[..startIndex];
        FileType         = Mp3File.AudioFormat;
        Bitrate          = Mp3File.Bitrate;
        IsVbr            = Mp3File.IsVBR;
        DurationSeconds  = Mp3File.Duration;
        SampleRate       = Mp3File.SampleRate;
        BitDepth         = Mp3File.BitDepth;

        // ID3-Tags direkt laden
        LoadID3TagsFromFile();
        WasManipulated = false;
        CaptureEditBaseline();
    }


    #region The TagLib#-Music file(s)

    public Track Mp3File { get; set; } = null!;

    private EditBaseline? _editBaseline;

    private sealed class EditBaseline {
        public required string FileName                { get; init; }
        public required string ContainingDirectoryName { get; init; }
        public required string Title                   { get; init; }
        public required string Album                   { get; init; }
        public required string Artist                  { get; init; }
        public string?         AlbumArtist             { get; init; }
        public required string Comment                 { get; init; }
        public required string Genre                   { get; init; }
        public int?            Year                    { get; init; }
        public required string Moods                   { get; init; }
    }

    public string? EditBaselineFullPath =>
        _editBaseline is null
            ? null
            : Path.Combine(_editBaseline.ContainingDirectoryName, _editBaseline.FileName);

    public void CaptureEditBaseline() {
        _editBaseline = new EditBaseline {
            FileName                = FileName,
            ContainingDirectoryName = ContainingDirectoryName,
            Title                   = _id3Title ?? "",
            Album                   = _id3Album ?? "",
            Artist                  = JoinTags(_id3Artists),
            AlbumArtist             = Mp3File?.AlbumArtist,
            Comment                 = _id3Comment ?? "",
            Genre                   = JoinTags(_id3Genres),
            Year                    = _id3ReleaseYear,
            Moods                   = JoinTags(_id3Moods),
        };
    }

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


    private string _fileName = "";

    public string FileName {
        get => _fileName;
        set {
            _fileName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FullPath));
        }
    }


    private string _containingDirectoryName = "";

    public string ContainingDirectoryName {
        get => _containingDirectoryName;
        set {
            _containingDirectoryName = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FullPath));
        }
    }

    public string FullPath => Path.Combine(ContainingDirectoryName, FileName);


    private AudioFormat _fileType = null!;

    public AudioFormat FileType {
        get => _fileType;
        set {
            _fileType = value;
            OnPropertyChanged();
        }
    }

    public int    Bitrate         { get; }
    public bool   IsVbr           { get; }
    public int    DurationSeconds { get; }
    public double SampleRate      { get; }
    public int    BitDepth        { get; }

    public string DurationText =>
        TimeSpan.FromSeconds(Math.Max(0, DurationSeconds))
            .ToString(DurationSeconds >= 3600 ? @"h\:mm\:ss" : @"m\:ss");

    public string BitrateText {
        get {
            if (Bitrate <= 0)
                return "—";
            var text = $"{Bitrate} kBit/s";
            return IsVbr ? $"{text} VBR" : text;
        }
    }

    public string SampleRateText {
        get {
            if (SampleRate <= 0)
                return "—";
            return SampleRate >= 1000
                ? $"{SampleRate / 1000.0:0.###} kHz"
                : $"{SampleRate:0} Hz";
        }
    }

    public string BitDepthText => BitDepth > 0 ? $"{BitDepth} bit" : "—";

    public string ArtistsDisplay =>
        string.Join(", ", Id3ArtistCollection.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string GenresDisplay =>
        string.Join(", ", Id3GenreCollection.Where(s => !string.IsNullOrWhiteSpace(s)));

    public string MoodsDisplay =>
        string.Join(", ", Id3MoodCollection.Where(s => !string.IsNullOrWhiteSpace(s)));

    #endregion


    #region ID3 Properties

    private string _id3Title = "";

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


    private string _id3Album = "";

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
            var year = NormalizeYear(value);
            if (_id3ReleaseYear == year)
                return;
            _id3ReleaseYear = year;
            WriteReleaseYear(Mp3File, year);
            Mp3File.Save();
            OnPropertyChanged();
        }
    }


    private string _id3Comment = "";

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


    private ObservableCollection<string> _id3Artists = [];

    public ObservableCollection<string> Id3ArtistCollection {
        get => _id3Artists;
        set {
            if (ReferenceEquals(_id3Artists, value)) return;
            _id3Artists    = value ?? [];
            Mp3File.Artist = JoinTags(Id3ArtistCollection);
            Mp3File.Save();
            OnPropertyChanged();
            OnPropertyChanged(nameof(ArtistsDisplay));
        }
    }


    private ObservableCollection<string> _id3Genres = [];

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


    private ObservableCollection<string> _id3Moods = [];

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


    public bool ApplyResolvedCatalog(
        IReadOnlyList<string> artists,
        IReadOnlyList<string> genres,
        IReadOnlyList<string> moods) {
        if (SameTags(_id3Artists, artists) && SameTags(_id3Genres, genres) && SameTags(_id3Moods, moods))
            return false;

        _id3Artists = new ObservableCollection<string>(artists);
        _id3Genres  = new ObservableCollection<string>(genres);
        _id3Moods   = new ObservableCollection<string>(moods);
        Mp3File.Artist = JoinTags(_id3Artists);
        Mp3File.Genre  = JoinTags(_id3Genres);
        Mp3File.AdditionalFields["MOOD"] = JoinTags(_id3Moods);
        Mp3File.AdditionalFields["TMOO"] = JoinTags(_id3Moods);
        Mp3File.Save();
        OnPropertyChanged(nameof(Id3ArtistCollection));
        OnPropertyChanged(nameof(ArtistsDisplay));
        OnPropertyChanged(nameof(Id3GenreCollection));
        OnPropertyChanged(nameof(Id3MoodCollection));
        return true;
    }

    private static bool SameTags(IReadOnlyList<string> left, IReadOnlyList<string> right) {
        if (left.Count != right.Count)
            return false;
        for (var i = 0; i < left.Count; i++) {
            if (!string.Equals(left[i], right[i], StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private void LoadID3TagsFromFile() {
        // Direkte Zuweisung ohne rekursive Setter
        _id3Title       = Mp3File.Title;
        _id3Album       = Mp3File.Album;
        _id3ReleaseYear = ReadReleaseYear(Mp3File);
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
            Mp3File.Title  = _id3Title;
            Mp3File.Album  = _id3Album;
            Mp3File.Artist = string.Join(';', _id3Artists);
            WriteReleaseYear(Mp3File, _id3ReleaseYear);
            Mp3File.Comment = _id3Comment;
            Mp3File.Genre               = JoinTags(_id3Genres);

            Mp3File.Save();
            WasManipulated = false;
            Debug.WriteLine($"Saved tags: {FileName}");
        } catch (Exception ex) {
            Debug.WriteLine($"Failed to save {FileName}: {ex.Message}");
            await BuranMessageBox.Show(Buran.Localization.L.Format("File.SaveFailed", FileName, ex.Message));
            throw;
        }
    }

    public bool RestoreEditBaselineTags() {
        if (Mp3File is null || _editBaseline is null)
            return false;

        var src = _editBaseline;
        Mp3File.Title       = src.Title;
        Mp3File.Album       = src.Album;
        Mp3File.Artist      = src.Artist;
        Mp3File.AlbumArtist = src.AlbumArtist;
        Mp3File.Comment     = src.Comment;
        Mp3File.Genre       = src.Genre;
        WriteReleaseYear(Mp3File, src.Year);
        Mp3File.AdditionalFields["MOOD"] = src.Moods;
        Mp3File.AdditionalFields["TMOO"] = src.Moods;

        LoadID3TagsFromFile();
        try {
            Mp3File.Save();
        }
        catch (Exception ex) {
            Debug.WriteLine($"Failed to reset tags: {FileName}: {ex.Message}");
            _ = BuranMessageBox.Show(Buran.Localization.L.Format("File.SaveFailed", FileName, ex.Message));
            return false;
        }

        WasManipulated = false;
        OnPropertyChanged(nameof(GenresDisplay));
        OnPropertyChanged(nameof(MoodsDisplay));
        return true;
    }

    public void RebindToPath(string newPath) {
        FileName                = Path.GetFileName(newPath);
        ContainingDirectoryName = Path.GetDirectoryName(newPath) ?? "";
        if (!File.Exists(newPath))
            return;
        Mp3File = new Track(newPath);
        LoadID3TagsFromFile();
        OnPropertyChanged(nameof(GenresDisplay));
        OnPropertyChanged(nameof(MoodsDisplay));
    }


    private void ParseAndSetArtistsFromTag() {
        if (string.IsNullOrEmpty(Mp3File.Artist)) {
            _id3Artists = [];
            OnPropertyChanged(nameof(Id3ArtistCollection));
            OnPropertyChanged(nameof(ArtistsDisplay));
            return;
        }

        ObservableCollection<string> allArtists = [];

        foreach (var artist in CollaborationMarkers.SplitArtistNames(Mp3File.Artist))
            allArtists.Add(artist);

        // Doppelte entfernen
        allArtists = new ObservableCollection<string>(allArtists.Distinct(StringComparer.OrdinalIgnoreCase).ToList());

        // Direkt zuweisen
        _id3Artists = allArtists;

        // Events auslösen
        OnPropertyChanged(nameof(Id3ArtistCollection));
        OnPropertyChanged(nameof(ArtistsDisplay));

        Debug.WriteLine($"Parsed artists for {FileName}: {string.Join(", ", allArtists)}");
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
            Logger.Error($"Failed to read moods: {ex.Message}");
            BuranMessageBox.Show(Buran.Localization.L.Format("File.ReadMoodsFailed", ex.Message)).Wait();
        }

        return moodVal;
    }


    public static int? ReadReleaseYear(Track? track) {
        if (track is null)
            return null;
        if (track.OriginalReleaseYear is > 0 and var original)
            return original;
        if (track.Year is > 0 and var year)
            return year;
        if (track.OriginalReleaseDate is DateTime originalDate && IsPlausibleYear(originalDate.Year))
            return originalDate.Year;
        if (track.Date is DateTime date && IsPlausibleYear(date.Year))
            return date.Year;
        return null;
    }

    public static void WriteReleaseYear(Track track, int? year) {
        var value = NormalizeYear(year);
        track.OriginalReleaseYear = value;
        track.Year                = value;
    }

    private static int? NormalizeYear(int? year) =>
        year is > 0 && IsPlausibleYear(year.Value) ? year : null;

    private static bool IsPlausibleYear(int year) => year is >= 1000 and <= 9999;

    private static ObservableCollection<string> SplitTags(string? raw) {
        var result = string.IsNullOrWhiteSpace(raw) ? [] : new ObservableCollection<string>(raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());
        return result;
    }

    private static string JoinTags(IEnumerable<string>? items) => items is null ? string.Empty : string.Join(';', items.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));


    private string _GenreToAdd = "";

    public string GenreToAdd {
        get => _GenreToAdd;
        set {
            _GenreToAdd = value;
            OnPropertyChanged();
        }
    }

    private string _ArtistToAdd = "";

    public string ArtistToAdd {
        get => _ArtistToAdd;
        set {
            _ArtistToAdd = value;
            OnPropertyChanged();
        }
    }

    private string _MoodToAdd = "";

    public string MoodToAdd {
        get => _MoodToAdd;
        set {
            _MoodToAdd = value;
            OnPropertyChanged();
        }
    }
}