using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Buran.Interfaces;
using Buran.SQLite;
using CommunityToolkit.Mvvm.Input;
using Buran.ID3Editor.Models;
using Buran.ID3Editor.Services;
using Buran.ID3Editor.Views;
using Buran.Types;
using Buran.Localization;
using CommunityToolkit.Mvvm.ComponentModel;


namespace Buran.ID3Editor.ViewModels;

public partial class Id3EditorTabViewModel : ViewModelBase {
    /// <summary>
    /// Constructor: Sets up Commands and NotifyPropertyChanged
    /// </summary>
    public Id3EditorTabViewModel() {
        _selectedPath = string.Empty;

        FileNameFromId3Command               =  new RelayCommand<object>(FileNameFromId3);
        Id3FromFileNameCommand               =  new RelayCommand<object>(Id3FromFileName!);
        RemoveSingleArtistFromID3TagsCommand =  new RelayCommand<object>(RemoveSingleArtistFromID3Tags);
        RemoveSingleGenreFromID3TagsCommand  =  new RelayCommand<object>(RemoveSingleGenreFromID3Tags);
        RemoveSingleMoodFromID3TagsCommand   =  new RelayCommand<object>(RemoveSingleMoodFromID3Tags);
        AddSingleArtistFromID3TagsCommand    =  new RelayCommand<Mp3FileObject>(AddSingleArtistFromID3Tags);
        AddSingleGenreFromID3TagsCommand     =  new RelayCommand<Mp3FileObject>(AddSingleGenreFromID3Tags);
        AddSingleMoodFromID3TagsCommand      =  new RelayCommand<Mp3FileObject>(AddSingleMoodFromID3Tags);
        ResetId3ToDefaultCommand             =  new RelayCommand<object>(ResetID3ToDefault!);
        OpenBulkAddDialogCommand             =  new RelayCommand<string>(OpenBulkAddDialog);
        OpenBulkCommentsDialogCommand        =  new RelayCommand(OpenBulkCommentsDialog, () => HasSelectedFiles);
        PlaySelectedCommand                  =  new RelayCommand(PlaySelected, () => HasSelectedFiles && HasPlayerModule);
        AddSelectedToPlaylistCommand         =  new RelayCommand(AddSelectedToPlaylist, () => HasSelectedFiles && HasPlayerModule);
        SelectAllFilesCommand                =  new RelayCommand(SelectAllFiles, () => HasMusicFiles);
        UnselectAllFilesCommand              =  new RelayCommand(UnselectAllFiles, () => HasSelectedFiles);
        BulkFileNameFromId3Command           =  new AsyncRelayCommand(BulkFileNameFromId3Async, () => HasSelectedFiles);
        BulkId3FromFileNameCommand           =  new RelayCommand(BulkId3FromFileName, () => HasSelectedFiles);
        MusicFiles                           =  new ObservableCollection<Buran.Types.Mp3FileObject>();
        InitColumns();
        MusicFiles.CollectionChanged         += (s, e) => {
            OnPropertyChanged(nameof(HasMusicFiles));
            NotifySelectionCommands();
        };
        MusicFiles.CollectionChanged         += OnMusicFilesCollectionChanged!;
        ReloadCatalogSuggestions();
        DBConnector.CatalogChanged           += OnCatalogChanged;
        L.WhenChanged(() => {
            OnPropertyChanged(nameof(SelectedFilesCountText));
            OnPropertyChanged(nameof(EditToggleLabel));
        });
    }

    private int _catalogReloadQueued;

    private void OnCatalogChanged(object? sender, EventArgs e) {
        if (Interlocked.Exchange(ref _catalogReloadQueued, 1) == 1)
            return;

        Dispatcher.UIThread.Post(() => {
            Interlocked.Exchange(ref _catalogReloadQueued, 0);
            ReloadCatalogSuggestions();
        });
    }

    public ObservableCollection<string> KnownArtistNames { get; } = new();
    public ObservableCollection<string> KnownGenreNames  { get; } = new();
    public ObservableCollection<string> KnownMoodNames   { get; } = new();

    public void ReloadCatalogSuggestions() {
        try {
            Replace(KnownArtistNames, CatalogNameCache.LoadArtistSuggestionNames());
            Replace(KnownGenreNames,  CatalogNameCache.LoadGenreSuggestionNames());
            Replace(KnownMoodNames,   CatalogNameCache.LoadMoodSuggestionNames());
        } catch (Exception ex) {
            Debug.WriteLine($"Catalog suggestions could not be loaded: {ex.Message}");
        }
    }

    private static void Replace(ObservableCollection<string> target, IEnumerable<string> source) {
        target.Clear();
        foreach (var item in source)
            target.Add(item);
    }


    #region PropertyChanged

    private void OnMusicFilesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        // Neue Files: PropertyChanged für IsSelected abonnieren
        if (e.NewItems != null) {
            foreach (Mp3FileObject file in e.NewItems) {
                file.PropertyChanged += OnFilePropertyChanged;
            }
        }

        // Entfernte Files: Event abbestellen
        if (e.OldItems != null) {
            foreach (Mp3FileObject file in e.OldItems) {
                file.PropertyChanged -= OnFilePropertyChanged;
            }
        }

        OnPropertyChanged(nameof(HasSelectedFiles));
        OnPropertyChanged(nameof(SelectedFilesCount));
        OnPropertyChanged(nameof(SelectedFilesCountText));
        OnPropertyChanged(nameof(ShowBulkBar));
        NotifySelectionCommands();
    }

    private void OnFilePropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName != nameof(Mp3FileObject.IsSelected)) return;
        OnPropertyChanged(nameof(HasSelectedFiles));
        OnPropertyChanged(nameof(SelectedFilesCount));
        OnPropertyChanged(nameof(SelectedFilesCountText));
        OnPropertyChanged(nameof(ShowBulkBar));
        NotifySelectionCommands();
    }

    #endregion


    [ObservableProperty] private string _selectedPath;
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private bool _hasPlayerDock;
    [ObservableProperty] private Mp3FileObject? _focusedFile;
    [ObservableProperty] private string _filterCaption = "";
    [ObservableProperty] private bool _isLoadingFiles;

    public bool HasFilterCaption => !string.IsNullOrEmpty(FilterCaption);

    partial void OnFilterCaptionChanged(string value) =>
        OnPropertyChanged(nameof(HasFilterCaption));

    [RelayCommand]
    private void ClearFilter() {
        FilterCaption = "";
        if (ModuleHub.TryRestoreLibraryFolder())
            return;
        MusicFiles.Clear();
        FocusedFile = null;
        SelectedPath = "";
    }

    [RelayCommand]
    private void ToggleColumn(TrackColumnOption? column) {
        if (column is null)
            return;
        column.IsVisible = !column.IsVisible;
        ColumnsChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool ShowBulkBar => IsEditMode && HasSelectedFiles;

    [ObservableProperty] private double _inspectorPaneWidth = 320;

    public ObservableCollection<TrackColumnOption> ColumnOptions { get; } = [];

    public event EventHandler? ColumnsChanged;

    public string EditToggleLabel =>
        IsEditMode ? L.Get("Id3.Editing") : L.Get("Id3.EditTags");

    [RelayCommand]
    private void ToggleEditMode() => IsEditMode = !IsEditMode;

    partial void OnIsEditModeChanged(bool value) {
        OnPropertyChanged(nameof(ShowBulkBar));
        OnPropertyChanged(nameof(EditToggleLabel));
    }

    public void NotifyPlayerAvailable() {
        OnPropertyChanged(nameof(HasPlayerModule));
        NotifySelectionCommands();
    }

    private ObservableCollection<Mp3FileObject> _musicFiles = [];

    public ObservableCollection<Mp3FileObject> MusicFiles {
        get => _musicFiles;
        set {
            _musicFiles = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMusicFiles));
        }
    }

    public bool HasMusicFiles => MusicFiles?.Count > 0;



    #region LoadMusicFiles

    public void LoadFromFolder(string folderPath, bool includeSubfolders = false) {
        if (string.IsNullOrWhiteSpace(folderPath))
            return;
        SelectedPath = folderPath.Replace("%20", " ");
        FilterCaption = includeSubfolders ? L.Get("Id3.IncludingSubfolders") : "";
        _ = LoadPathsAsync(EnumerateAudio(SelectedPath, includeSubfolders), index: true, promptCatalog: true);
    }

    public void LoadFromPaths(IReadOnlyList<string> paths, string caption) {
        FilterCaption = caption;
        EnsureColumnVisible("FolderPath");
        _ = LoadPathsAsync(Task.FromResult(paths.ToList()), index: false, promptCatalog: false);
    }

    public void StartLibraryIndex(IReadOnlyList<string> rootPaths) =>
        _ = IndexLibraryAsync(rootPaths);

    public void LoadMusicFiles() =>
        LoadFromFolder(SelectedPath, includeSubfolders: false);

    private CancellationTokenSource? _loadCts;

    private async Task LoadPathsAsync(Task<List<string>> pathsTask, bool index, bool promptCatalog) {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        IsLoadingFiles = true;
        MusicFiles.Clear();
        try {
            var paths = await pathsTask.ConfigureAwait(true);
            token.ThrowIfCancellationRequested();
            const int batch = 16;
            var buffer = new List<Mp3FileObject>(batch);
            foreach (var path in paths) {
                token.ThrowIfCancellationRequested();
                var mp3 = await Task.Run(() => {
                    var file = new Mp3FileObject(path);
                    NormalizeCatalogNames(file);
                    if (index)
                        IndexFile(file);
                    return file;
                }, token).ConfigureAwait(true);
                buffer.Add(mp3);
                if (buffer.Count >= batch) {
                    foreach (var item in buffer)
                        MusicFiles.Add(item);
                    buffer.Clear();
                }
            }

            foreach (var item in buffer)
                MusicFiles.Add(item);
            FocusedFile = MusicFiles.FirstOrDefault();
            if (promptCatalog)
                PromptForNewCatalogValues();
        }
        catch (OperationCanceledException) {
            // Newer load replaced this one.
        }
        finally {
            IsLoadingFiles = false;
        }
    }

    private static Task<List<string>> EnumerateAudio(string folder, bool recursive) =>
        Task.Run(() => {
            var found = new List<string>();
            CollectAudio(folder, recursive, found);
            found.Sort(StringComparer.CurrentCultureIgnoreCase);
            return found;
        });

    private static void CollectAudio(string folder, bool recursive, List<string> acc) {
        try {
            foreach (var file in Directory.EnumerateFiles(folder)) {
                if (file.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
                    acc.Add(file);
            }

            if (!recursive)
                return;
            foreach (var dir in Directory.EnumerateDirectories(folder))
                CollectAudio(dir, true, acc);
        }
        catch {
            // Unreadable folder.
        }
    }

    public async Task IndexLibraryAsync(IReadOnlyList<string> rootPaths) {
        IsLoadingFiles = true;
        try {
            foreach (var rootPath in rootPaths) {
                if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                    continue;
                var paths = await EnumerateAudio(rootPath, recursive: true).ConfigureAwait(true);
                foreach (var path in paths) {
                    await Task.Run(() => {
                        try {
                            var file = new Mp3FileObject(path);
                            IndexFile(file);
                        }
                        catch {
                            // Skip unreadable files.
                        }
                    }).ConfigureAwait(true);
                }
            }
        }
        finally {
            IsLoadingFiles = false;
        }
    }

    private static void IndexFile(Mp3FileObject file) {
        try {
            DBConnector.UpsertIndexedTrack(
                file.FullPath,
                file.Id3Title,
                string.Join(';', file.Id3ArtistCollection),
                file.Id3Album,
                file.Id3ReleaseYear,
                string.Join(';', file.Id3GenreCollection),
                string.Join(';', file.Id3MoodCollection),
                file.DurationSeconds,
                file.Bitrate,
                file.SampleRate,
                file.BitDepth);
        }
        catch (Exception ex) {
            Debug.WriteLine($"Index failed for {file.FullPath}: {ex.Message}");
        }
    }

    private void InitColumns() {
        TrackColumnOption[] defaults = [
            new() { Id = "Artists",    HeaderKey = "Id3.Artists",     Binding = "ArtistsDisplay",           IsVisible = true },
            new() { Id = "Title",      HeaderKey = "Id3.Title",       Binding = "Id3Title",                 IsVisible = true },
            new() { Id = "FolderPath", HeaderKey = "Id3.FolderPath",  Binding = "ContainingDirectoryName",  IsVisible = true },
            new() { Id = "Album",      HeaderKey = "Id3.Album",       Binding = "Id3Album",                 IsVisible = true },
            new() { Id = "Year",       HeaderKey = "Id3.ReleaseYear", Binding = "Id3ReleaseYear",  IsVisible = true },
            new() { Id = "Duration",   HeaderKey = "Id3.Duration",    Binding = "DurationText",    IsVisible = true },
            new() { Id = "Bitrate",    HeaderKey = "Id3.Bitrate",     Binding = "BitrateText",     IsVisible = true },
            new() { Id = "SampleRate", HeaderKey = "Id3.SampleRate",  Binding = "SampleRateText",  IsVisible = true },
            new() { Id = "BitDepth",   HeaderKey = "Id3.BitDepth",    Binding = "BitDepthText",    IsVisible = true },
            new() { Id = "Filename",   HeaderKey = "Id3.Filename",    Binding = "FileName",        IsVisible = false },
            new() { Id = "Genre",      HeaderKey = "Id3.Genre",       Binding = "GenresDisplay",    IsVisible = false },
            new() { Id = "Mood",       HeaderKey = "Id3.Moods",       Binding = "MoodsDisplay",     IsVisible = false },
        ];
        foreach (var column in defaults)
            ColumnOptions.Add(column);
    }

    private static void NormalizeCatalogNames(Mp3FileObject file) {
        static List<string> Map(IEnumerable<string>? values, Func<string, CatalogNameResolution> resolve) =>
            (values ?? [])
                .Select(v => resolve(v).PreferredName)
                .Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        file.ApplyResolvedCatalog(
            Map(file.Id3ArtistCollection, DBConnector.ResolveArtistName),
            Map(file.Id3GenreCollection, DBConnector.ResolveGenreName),
            Map(file.Id3MoodCollection, DBConnector.ResolveMoodName));
    }

    private void PromptForNewCatalogValues() {
        if (MusicFiles.Count == 0)
            return;

        var suggestions = CatalogSuggestionCollector.Collect(MusicFiles);
        if (suggestions.Count == 0)
            return;

        var dialog = new ImportCatalogSuggestionsWindow();
        var vm     = new ImportCatalogSuggestionsViewModel(suggestions);
        dialog.DataContext = vm;
        vm.CloseRequested += (_, _) => {
            dialog.Close();
            ReloadCatalogSuggestions();
        };

        var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (owner is not null)
            dialog.ShowDialog(owner);
        else
            dialog.Show();
    }

    #endregion


    #region ID3 To Filename

    public ICommand FileNameFromId3Command { get; set; }

    /// <summary>
    /// Should just take all Artists from the ID3-Tag and inserts them into the file name.
    /// Checks like for preferred names and similar stuff is not part of this method.
    /// </summary>
    /// <param name="sender"></param>
    private async void FileNameFromId3(object? sender) {
        if (sender is not Mp3FileObject file)
            return;

        var error = await RenameFromId3Async(file);
        if (error is not null)
            await BuranMessageBox.Show(error);
    }

    private async Task BulkFileNameFromId3Async() {
        var errors = new List<string>();
        foreach (var file in MusicFiles.Where(f => f.IsSelected).ToList()) {
            var error = await RenameFromId3Async(file);
            if (error is not null)
                errors.Add($"{file.FileName}: {error}");
        }

        if (errors.Count > 0)
            await BuranMessageBox.Show(string.Join("\n", errors), L.Get("Id3.RenameCaption"));
    }

    private async Task<string?> RenameFromId3Async(Mp3FileObject file) {
        try {
            if (file.Mp3File is null)
                return L.Get("Id3.CouldNotReadFile");

            var artistString = GetArtistNamesFromId3AsFormattedString(file);

            var title      = GetSongTitleFromId3(file);
            var original   = file.FileName;
            var extension  = Path.GetExtension(original);
            if (string.IsNullOrEmpty(extension)) {
                var mime = file.FileType?.MimeList?.FirstOrDefault();
                extension = mime is not null && mime.Contains('/')
                    ? "." + mime[(mime.IndexOf('/') + 1)..]
                    : ".mp3";
            }

            var newName = artistString + " - " + title + extension;
            if (string.Equals(original, newName, StringComparison.Ordinal))
                return null;

            var directory = file.ContainingDirectoryName;
            var oldPath   = Path.Combine(directory, original);
            var newPath   = Path.Combine(directory, newName);

            file.Mp3File.Save();

            if (File.Exists(newPath) && !PathsEqual(oldPath, newPath)) {
                var decision = await ShowCompareFilesDialogAsync(file, newPath);
                switch (decision) {
                    case CompareFilesDecision.KeepBoth:
                        return null;
                    case CompareFilesDecision.UseExisting:
                        return DeleteCurrentFile(file, oldPath);
                    case CompareFilesDecision.UseCurrent:
                        var replaceError = DeleteExistingTarget(newPath);
                        if (replaceError is not null)
                            return replaceError;
                        break;
                }
            }

            try {
                File.Move(oldPath, newPath);
            } catch (IOException ex) {
                if (File.Exists(newPath) && !PathsEqual(oldPath, newPath)) {
                    var decision = await ShowCompareFilesDialogAsync(file, newPath);
                    switch (decision) {
                        case CompareFilesDecision.KeepBoth:
                            return null;
                        case CompareFilesDecision.UseExisting:
                            return DeleteCurrentFile(file, oldPath);
                        case CompareFilesDecision.UseCurrent: {
                            var replaceError = DeleteExistingTarget(newPath);
                            if (replaceError is not null)
                                return replaceError;
                            File.Move(oldPath, newPath);
                            break;
                        }
                    }
                } else {
                    return ex.Message;
                }
            }

            ReplaceListEntry(file, newPath);
            return null;
        } catch (Exception ex) {
            return ex.Message;
        }
    }

    private async Task<CompareFilesDecision> ShowCompareFilesDialogAsync(Mp3FileObject current, string existingPath) {
        var vm     = new CompareFilesViewModel(current, existingPath);
        var dialog = new CompareFilesWindow { DataContext = vm };
        vm.CloseRequested += (_, _) => dialog.Close();

        var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (owner is not null)
            await dialog.ShowDialog(owner);
        else {
            var closed = new TaskCompletionSource();
            dialog.Closed += (_, _) => closed.TrySetResult();
            dialog.Show();
            await closed.Task;
        }

        return vm.Decision;
    }

    private string? DeleteExistingTarget(string path) {
        try {
            File.Delete(path);
        } catch (Exception ex) {
            return L.Format("Id3.RenameDeleteFailed", ex.Message);
        }

        RemoveByFullPath(path);
        return null;
    }

    private string? DeleteCurrentFile(Mp3FileObject file, string path) {
        try {
            File.Delete(path);
        } catch (Exception ex) {
            return L.Format("Id3.RenameDeleteFailed", ex.Message);
        }

        MusicFiles.Remove(file);
        return null;
    }

    private void ReplaceListEntry(Mp3FileObject file, string newPath) {
        var selected = file.IsSelected;
        var index    = MusicFiles.IndexOf(file);
        var replacement = new Mp3FileObject(newPath) { IsSelected = selected };
        if (index >= 0)
            MusicFiles[index] = replacement;
        else
            MusicFiles.Add(replacement);
    }

    private void RemoveByFullPath(string path) {
        foreach (var item in MusicFiles.Where(f => PathsEqual(f.FullPath, path)).ToList())
            MusicFiles.Remove(item);
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.Ordinal);


    /// <summary>
    /// Pulls the title from the ID3 tags and returns it
    /// </summary>
    /// <param name="fileObject"></param>
    /// <returns>[string]</returns>
    private string GetSongTitleFromId3(Mp3FileObject fileObject) {
        return fileObject.Id3Title;
    }


    /// <summary>
    /// pulls all artists from the ID3 tags and puts them in a format that is filename friendly.
    /// </summary>
    /// <param name="fileObject"></param>
    /// <returns>[string] with all Artists, formatted in a filename friendly way</returns>
    private string GetArtistNamesFromId3AsFormattedString(Mp3FileObject fileObject) {
        List<string>  preferredArtistNameList = [];
        StringBuilder sb                      = new StringBuilder();

        // Assembling ArtistListing
        foreach (string artist in fileObject.Id3ArtistCollection) {
            string preferred = DBConnector.ResolveArtistName(artist).PreferredName;
            preferredArtistNameList.Add(preferred);
        }


        // from here, work with preferredArtistNameList!
        if (preferredArtistNameList.Count == 2) {
            // Nur 2 Artists: "Artist1 feat. Artist2"
            sb.Append(preferredArtistNameList[0]);
            sb.Append(" feat. ");
            sb.Append(preferredArtistNameList[1]);
        } else if (preferredArtistNameList.Count > 2) {
            // Mehr als 2 Artists
            for (int i = 0; i < preferredArtistNameList.Count; i++) {
                sb.Append(preferredArtistNameList[i]);

                if (i < preferredArtistNameList.Count - 1) {
                    if (i == 0)
                        sb.Append(" feat. "); // Erster zu Zweitem
                    else if (i == preferredArtistNameList.Count - 2)
                        sb.Append(" & "); // Vorletzter zu Letztem
                    else
                        sb.Append(", "); // Alle dazwischen
                }
            }
        } else if (preferredArtistNameList.Count == 1) {
            sb.Append(preferredArtistNameList[0]);
        }

        return sb.ToString();
    }

    #endregion


    #region Filename To ID3

    public ICommand Id3FromFileNameCommand { get; set; }

    public void Id3FromFileName(object sender) {
        ArgumentNullException.ThrowIfNull(sender);
        if (sender is not Mp3FileObject file) return;

        var parser   = new FileNameParser();
        var patterns = DBConnector.LoadFileNamePatterns();
        ApplyId3FromFileName(file, parser, patterns);
    }

    private void BulkId3FromFileName() {
        var parser   = new FileNameParser();
        var patterns = DBConnector.LoadFileNamePatterns();
        foreach (var file in MusicFiles.Where(f => f.IsSelected).ToList())
            ApplyId3FromFileName(file, parser, patterns);

        PromptForNewCatalogValues();
    }

    private static void ApplyId3FromFileName(
        Mp3FileObject file,
        FileNameParser parser,
        ObservableCollection<DBConnector.FileNamePattern> patterns) {
        var metadata = parser.Parse(file.FileName, patterns);

        if (metadata.MatchedPatternId is int patternId)
            DBConnector.IncreasePatternConfidence(patternId);
        else
            parser.LearnNewPattern(file.FileName, metadata);

        ApplyParsedMetadata(file, metadata);
        file.SaveTags();
    }

    private static void ApplyParsedMetadata(Mp3FileObject file, ParsedMetadata metadata) {
        if (!string.IsNullOrEmpty(metadata.Title))
            file.Id3Title = metadata.Title;

        if (metadata.Artists is { Count: > 0 }) {
            var resolved = metadata.Artists
                .Select(a => DBConnector.ResolveArtistName(a).PreferredName)
                .Where(n => n.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            file.Id3ArtistCollection = new ObservableCollection<string>(resolved);
        }

        if (!string.IsNullOrEmpty(metadata.Album))
            file.Id3Album = metadata.Album;

        if (metadata.Comments is { Count: > 0 })
            file.Id3Comment = string.Join("; ", metadata.Comments);

        if (int.TryParse(metadata.Year, out var year) && year is >= 1000 and <= 9999)
            file.Id3ReleaseYear = year;
    }

    #endregion


    #region ResetToDefaultValues

    public ICommand ResetId3ToDefaultCommand { get; set; }

    public void ResetID3ToDefault(object sender) {
        ArgumentNullException.ThrowIfNull(sender);
        Mp3FileObject sndr = (Mp3FileObject)sender;
        sndr.Mp3File = sndr.Mp3FileInInitialState;
    }

    #endregion


    #region Bulk_AddTags

    public ICommand OpenBulkAddDialogCommand      { get; }
    public ICommand OpenBulkCommentsDialogCommand { get; }
    public ICommand PlaySelectedCommand           { get; }
    public ICommand AddSelectedToPlaylistCommand  { get; }
    public ICommand SelectAllFilesCommand         { get; }
    public ICommand UnselectAllFilesCommand       { get; }
    public ICommand BulkFileNameFromId3Command    { get; }
    public ICommand BulkId3FromFileNameCommand    { get; }

    public bool HasPlayerModule =>
        ModuleHub.Find<IPlaybackController>() is not null;

    public bool HasSelectedFiles   => MusicFiles?.Any(f => f.IsSelected    == true) ?? false;
    public int  SelectedFilesCount => MusicFiles?.Count(f => f?.IsSelected == true) ?? 0;
    public string SelectedFilesCountText => L.Format("Id3.SelectedCount", SelectedFilesCount);

    private void SelectAllFiles() {
        if (MusicFiles is null) return;
        foreach (var file in MusicFiles)
            file.IsSelected = true;
    }

    private void UnselectAllFiles() {
        if (MusicFiles is null) return;
        foreach (var file in MusicFiles)
            file.IsSelected = false;
    }

    private void NotifySelectionCommands() {
        (SelectAllFilesCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (UnselectAllFilesCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (BulkFileNameFromId3Command as IRelayCommand)?.NotifyCanExecuteChanged();
        (BulkId3FromFileNameCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (OpenBulkCommentsDialogCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (PlaySelectedCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        (AddSelectedToPlaylistCommand as IRelayCommand)?.NotifyCanExecuteChanged();
    }

    private void OpenBulkCommentsDialog() {
        var selectedFiles = MusicFiles.Where(f => f.IsSelected).ToList();
        if (selectedFiles.Count == 0)
            return;

        var dialog = new BulkCommentsWindow();
        var vm     = new BulkCommentsViewModel(selectedFiles);
        dialog.DataContext = vm;
        vm.CloseRequested += (_, _) => dialog.Close();

        var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (owner is not null)
            dialog.ShowDialog(owner);
        else
            dialog.Show();
    }

    private void PlaySelected() {
        var paths = SelectedPaths();
        if (paths.Count == 0)
            return;
        ModuleHub.Find<IPlaybackController>()?.PlayFile(paths[0], paths);
    }

    [RelayCommand]
    private void PlayThisFile(Mp3FileObject? file) {
        if (file is null || !HasPlayerModule)
            return;
        var queue = MusicFiles.Select(f => f.FullPath).ToList();
        ModuleHub.Find<IPlaybackController>()?.PlayFile(file.FullPath, queue);
    }

    [RelayCommand(CanExecute = nameof(CanOpenContainingFolder))]
    private void OpenContainingFolder() {
        var dir = FocusedFile?.ContainingDirectoryName;
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return;
        ModuleHub.ShowLibraryFolder(dir);
    }

    private bool CanOpenContainingFolder() =>
        FocusedFile is { ContainingDirectoryName.Length: > 0 };

    partial void OnFocusedFileChanged(Mp3FileObject? value) =>
        OpenContainingFolderCommand.NotifyCanExecuteChanged();

    private void EnsureColumnVisible(string id) {
        var column = ColumnOptions.FirstOrDefault(c => c.Id == id);
        if (column is not { IsVisible: false })
            return;
        column.IsVisible = true;
        ColumnsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddSelectedToPlaylist() {
        var paths = SelectedPaths();
        if (paths.Count == 0)
            return;
        ModuleHub.Find<IPlaylistSink>()?.AddTracks(paths);
    }

    private List<string> SelectedPaths() =>
        MusicFiles.Where(f => f.IsSelected).Select(f => f.FullPath).ToList();

    private void OpenBulkAddDialog(string? kind) {
        if (!Enum.TryParse(kind, ignoreCase: true, out CatalogSuggestionKind tagKind))
            return;

        var selectedFiles = MusicFiles.Where(f => f.IsSelected).ToList();
        if (selectedFiles.Count == 0)
            return;

        var dialog = new BulkAddTagsWindow();
        var vm     = new BulkAddTagsViewModel(selectedFiles, tagKind);
        dialog.DataContext = vm;
        vm.CloseRequested += (_, _) => {
            dialog.Close();
            ReloadCatalogSuggestions();
            OnPropertyChanged(nameof(MusicFiles));
        };

        var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        if (owner is not null)
            dialog.ShowDialog(owner);
        else
            dialog.Show();
    }

    #endregion


    // TODO: Check for DB-Entries. Maybe the Genre is already existent.

    #region Artist

    public ICommand AddSingleArtistFromID3TagsCommand    { get; set; }
    public ICommand RemoveSingleArtistFromID3TagsCommand { get; set; }

    public void AddSingleArtistFromID3Tags(Mp3FileObject? sender) {
        if (sender is null) return;
        var input = sender.ArtistToAdd?.Trim();
        if (string.IsNullOrWhiteSpace(input))
            return;

        var resolved = DBConnector.CheckForPreferredName(input);
        if (resolved.ArtistNameStatus == ArtistNameStatus.IsNonExistent &&
            !DBConnector.IsCatalogValueBlocked(DatabaseTable_BlockedCatalogValues.KindArtist, resolved.PreferredArtistName)) {
            DBConnector.InsertArtistName(resolved.PreferredArtistName, "");
            ReloadCatalogSuggestions();
        }

        var name = resolved.PreferredArtistName;
        var current = sender.Id3ArtistCollection ?? [];
        if (current.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase))) {
            sender.ArtistToAdd = string.Empty;
            return;
        }

        sender.Id3ArtistCollection = new ObservableCollection<string>(current.Append(name));
        sender.ArtistToAdd         = string.Empty;
    }

    public void RemoveSingleArtistFromID3Tags(object? parameter) {
        if (parameter is not ArtistRemoveArgs args) return;
        args.File.Id3ArtistCollection = WithoutTag(args.File.Id3ArtistCollection, args.Artist);
    }

    #endregion


    #region Genre

    public ICommand AddSingleGenreFromID3TagsCommand    { get; set; }
    public ICommand RemoveSingleGenreFromID3TagsCommand { get; set; }

    public void AddSingleGenreFromID3Tags(Mp3FileObject? sender) {
        if (sender is null) return;
        if (!TryAddResolvedTag(sender.GenreToAdd, DBConnector.ResolveGenreName, DBConnector.InsertGenreName,
                DatabaseTable_BlockedCatalogValues.KindGenre, out var name))
            return;

        var current = sender.Id3GenreCollection ?? [];
        if (current.Any(g => g.Equals(name, StringComparison.OrdinalIgnoreCase))) {
            sender.GenreToAdd = string.Empty;
            return;
        }

        sender.Id3GenreCollection = new ObservableCollection<string>(current.Append(name));
        sender.GenreToAdd         = string.Empty;
    }

    public void RemoveSingleGenreFromID3Tags(object? parameter) {
        if (parameter is not GenreRemoveArgs args) return;
        args.File.Id3GenreCollection = WithoutTag(args.File.Id3GenreCollection, args.Genre);
    }

    #endregion


    #region Moods

    public ICommand AddSingleMoodFromID3TagsCommand    { get; set; }
    public ICommand RemoveSingleMoodFromID3TagsCommand { get; set; }

    public void AddSingleMoodFromID3Tags(Mp3FileObject? sender) {
        if (sender is null) return;
        if (!TryAddResolvedTag(sender.MoodToAdd, DBConnector.ResolveMoodName, DBConnector.InsertMoodName,
                DatabaseTable_BlockedCatalogValues.KindMood, out var name))
            return;

        var current = sender.Id3MoodCollection ?? [];
        if (current.Any(m => m.Equals(name, StringComparison.OrdinalIgnoreCase))) {
            sender.MoodToAdd = string.Empty;
            return;
        }

        sender.Id3MoodCollection = new ObservableCollection<string>(current.Append(name));
        sender.MoodToAdd         = string.Empty;
    }

    private bool TryAddResolvedTag(
        string? raw,
        Func<string, CatalogNameResolution> resolve,
        Action<string> insert,
        string kind,
        out string name) {
        var trimmed = raw?.Trim() ?? string.Empty;
        name = trimmed;
        if (string.IsNullOrWhiteSpace(trimmed))
            return false;

        var resolved = resolve(trimmed);
        name = resolved.PreferredName;
        if (resolved.IsKnown)
            return true;

        if (DBConnector.IsCatalogValueBlocked(kind, name))
            return true;

        insert(name);
        ReloadCatalogSuggestions();
        return true;
    }

    public void RemoveSingleMoodFromID3Tags(object? parameter) {
        if (parameter is not MoodRemoveArgs args) return;
        args.File.Id3MoodCollection = WithoutTag(args.File.Id3MoodCollection, args.Mood);
    }

    private static ObservableCollection<string> WithoutTag(IEnumerable<string>? tags, string? value) {
        var remaining = (tags ?? []).Where(t => !string.Equals(t, value, StringComparison.Ordinal));
        return new ObservableCollection<string>(remaining);
    }

    #endregion
}