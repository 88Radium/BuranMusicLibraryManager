using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Buran.Interfaces;
using Buran.Localization;
using BuranUI.Models;
using BuranUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuranUI.ViewModels;

public partial class LibraryBrowserViewModel : ViewModelBase {
    private readonly UiSettings _settings;
    private          bool       _isInitializing = true;
    private          string?    _lastLoadedFolder;

    public LibraryBrowserViewModel(UiSettings settings) {
        _settings = settings;
        FolderNodes = new ObservableCollection<FolderNode>();

        var root = settings.LibraryRoot;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) {
            var music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (Directory.Exists(music))
                root = music;
        }

        if (!string.IsNullOrWhiteSpace(root) && Directory.Exists(root))
            _ = SetLibraryRootAsync(root, selectRoot: false);
        else
            _isInitializing = false;

        L.WhenChanged(() => {
            OnPropertyChanged(nameof(LibraryRootDisplay));
            OnPropertyChanged(nameof(CurrentFolderDisplay));
        });
    }

    public event EventHandler? RequestId3Tab;

    public ObservableCollection<FolderNode> FolderNodes { get; }

    [ObservableProperty] private string?     _libraryRoot;
    [ObservableProperty] private FolderNode? _selectedFolder;
    [ObservableProperty] private string?     _currentFolderPath;
    [ObservableProperty] private bool        _isLibraryLoading;

    public bool HasLibraryRoot => !string.IsNullOrEmpty(LibraryRoot);

    public string LibraryRootDisplay =>
        string.IsNullOrEmpty(LibraryRoot) ? L.Get("Library.NoneChosen") : LibraryRoot;

    public string CurrentFolderDisplay =>
        string.IsNullOrEmpty(CurrentFolderPath) ? L.Get("Library.NoFolder") : CurrentFolderPath;

    partial void OnLibraryRootChanged(string? value) {
        OnPropertyChanged(nameof(HasLibraryRoot));
        OnPropertyChanged(nameof(LibraryRootDisplay));
    }

    partial void OnCurrentFolderPathChanged(string? value) {
        OnPropertyChanged(nameof(CurrentFolderDisplay));
    }

    partial void OnSelectedFolderChanged(FolderNode? value) {
        if (value is null || value.IsPlaceholder)
            return;

        CurrentFolderPath = value.FullPath;
        NotifyMusicFolderConsumers(value.FullPath);
    }

    [RelayCommand]
    private async Task ChooseLibraryRoot() {
        var path = await FolderPickerService.PickFolderAsync(L.Get("Library.PickTitle"));
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        await SetLibraryRootAsync(path, selectRoot: true);
    }

    [RelayCommand]
    private async Task RefreshLibrary() {
        if (string.IsNullOrEmpty(LibraryRoot) || !Directory.Exists(LibraryRoot))
            return;

        await SetLibraryRootAsync(LibraryRoot, selectRoot: false);
    }

    private async Task SetLibraryRootAsync(string path, bool selectRoot) {
        IsLibraryLoading = true;
        try {
            var root = await Task.Run(() => FolderNode.FromPath(path)).ConfigureAwait(true);
            FolderNodes.Clear();
            FolderNodes.Add(root);
            LibraryRoot = path;

            _settings.LibraryRoot = path;
            _settings.Save();

            root.IsExpanded = true;
            await root.EnsureChildrenLoadedAsync();

            if (selectRoot) {
                _isInitializing = false;
                SelectedFolder  = root;
            }
        }
        finally {
            IsLibraryLoading = false;
            _isInitializing  = false;
        }
    }

    private void NotifyMusicFolderConsumers(string path) {
        if (string.Equals(_lastLoadedFolder, path, StringComparison.OrdinalIgnoreCase) && !_isInitializing)
            return;

        _lastLoadedFolder = path;

        var extensions = MainWindowViewModel.LoadedExtensions;
        if (extensions is not null) {
            foreach (var consumer in extensions.OfType<IMusicFolderConsumer>())
                consumer.LoadMusicFolder(path);
        }

        if (!_isInitializing)
            RequestId3Tab?.Invoke(this, EventArgs.Empty);
    }
}
