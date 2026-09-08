using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
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
    private          bool       _suppressFolderLoad;
    private          bool       _isPickingFolder;

    public LibraryBrowserViewModel(UiSettings settings) {
        _settings = settings;
        FolderNodes = new ObservableCollection<FolderNode>();
        _includeSubfolders = settings.IncludeSubfolders;
        ModuleHub.OpenLibraryFolder += OnOpenLibraryFolder;
        ModuleHub.RestoreLibraryFolderHandler = RestoreLibraryFolderView;

        var roots = settings.ResolvedLibraryRoots();
        if (roots.Count == 0) {
            var music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            if (Directory.Exists(music))
                roots = [music];
        }

        if (roots.Count > 0)
            _ = LoadRootsAsync(roots, selectFirst: false);
        else
            _isInitializing = false;

        L.WhenChanged(() => {
            OnPropertyChanged(nameof(LibraryRootDisplay));
            OnPropertyChanged(nameof(CurrentFolderDisplay));
        });
    }

    public event EventHandler? RequestId3Tab;

    public ObservableCollection<FolderNode> FolderNodes { get; }

    [ObservableProperty] private FolderNode? _selectedFolder;
    [ObservableProperty] private string?     _currentFolderPath;
    [ObservableProperty] private bool        _isLibraryLoading;
    [ObservableProperty] private bool        _includeSubfolders;
    [ObservableProperty] private bool        _isIndexing;

    public bool HasLibraryRoot => FolderNodes.Count > 0;

    public string? LibraryRoot => FolderNodes.FirstOrDefault()?.FullPath;

    public string LibraryRootDisplay => FolderNodes.Count switch {
        0 => L.Get("Library.NoneChosen"),
        1 => FolderNodes[0].FullPath,
        _ => L.Format("Library.RootCount", FolderNodes.Count)
    };

    public string LibraryRootsTooltip =>
        FolderNodes.Count == 0
            ? L.Get("Library.NoneChosen")
            : string.Join(Environment.NewLine, FolderNodes.Select(n => n.FullPath));

    public string CurrentFolderDisplay =>
        string.IsNullOrEmpty(CurrentFolderPath) ? L.Get("Library.NoFolder") : CurrentFolderPath;

    public bool CanRemoveLibraryRoot => RootOf(SelectedFolder) is not null;

    partial void OnCurrentFolderPathChanged(string? value) {
        OnPropertyChanged(nameof(CurrentFolderDisplay));
    }

    partial void OnSelectedFolderChanged(FolderNode? value) {
        OnPropertyChanged(nameof(CanRemoveLibraryRoot));
        if (_suppressFolderLoad || value is null || value.IsPlaceholder)
            return;

        CurrentFolderPath = value.FullPath;
        NotifyMusicFolderConsumers(value.FullPath, force: true);
    }

    private void OnOpenLibraryFolder(object? sender, string path) =>
        Dispatcher.UIThread.Post(() => _ = RevealAndSelectFolderAsync(path));

    private bool RestoreLibraryFolderView() {
        var target = SelectedFolder is { IsPlaceholder: false }
            ? SelectedFolder
            : FolderNodes.FirstOrDefault();
        if (target is null)
            return false;

        CurrentFolderPath = target.FullPath;
        if (!ReferenceEquals(SelectedFolder, target)) {
            _suppressFolderLoad = true;
            try {
                SelectedFolder = target;
            }
            finally {
                _suppressFolderLoad = false;
            }
        }

        NotifyMusicFolderConsumers(target.FullPath, force: true);
        return true;
    }

    public async Task RevealAndSelectFolderAsync(string folderPath) {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            return;

        var full = Path.GetFullPath(folderPath);
        FolderNode? node = null;
        var root = FolderNodes.FirstOrDefault(n => SamePath(n.FullPath, full) || IsAncestorOf(n.FullPath, full));
        if (root is not null)
            node = await ExpandToFolderAsync(root, full).ConfigureAwait(true);

        if (node is not null && SamePath(node.FullPath, full) && !ReferenceEquals(SelectedFolder, node)) {
            _suppressFolderLoad = true;
            try {
                SelectedFolder = node;
            }
            finally {
                _suppressFolderLoad = false;
            }
        }

        CurrentFolderPath = full;
        NotifyMusicFolderConsumers(full, force: true, includeSubfolders: false);
    }

    [RelayCommand]
    private async Task ChooseLibraryRoot() {
        if (_isPickingFolder)
            return;
        _isPickingFolder = true;
        try {
            var start  = SelectedFolder?.FullPath ?? LibraryRoot;
            var picked = await FolderPickerService.PickFoldersAsync(L.Get("Library.PickTitle"), start);
            if (picked.Count == 0)
                return;

            await AddRootsAsync(picked, selectNew: true);
        }
        finally {
            _isPickingFolder = false;
        }
    }

    [RelayCommand]
    private void RemoveLibraryRoot() {
        var root = RootOf(SelectedFolder);
        if (root is null)
            return;

        FolderNodes.Remove(root);
        PersistRoots();
        NotifyLibraryDisplay();

        if (FolderNodes.Count == 0) {
            SelectedFolder    = null;
            CurrentFolderPath = null;
            return;
        }

        if (RootOf(SelectedFolder) is null)
            SelectedFolder = FolderNodes[0];
    }

    [RelayCommand]
    private async Task RefreshLibrary() {
        if (FolderNodes.Count == 0)
            return;

        var selected = SelectedFolder?.FullPath;
        var paths    = FolderNodes.Select(n => n.FullPath).ToList();
        await LoadRootsAsync(paths, selectFirst: false);
        if (string.IsNullOrEmpty(selected))
            return;
        var root = FolderNodes.FirstOrDefault(n => SamePath(n.FullPath, selected) || IsAncestorOf(n.FullPath, selected));
        if (root is null)
            return;
        var node = await ExpandToFolderAsync(root, selected).ConfigureAwait(true);
        if (node is not null)
            SelectedFolder = node;
    }

    private async Task LoadRootsAsync(IReadOnlyList<string> paths, bool selectFirst) {
        IsLibraryLoading = true;
        try {
            var nodes = new List<FolderNode>();
            foreach (var path in NormalizeNewRoots(paths, existing: [])) {
                var node = await Task.Run(() => FolderNode.FromPath(path)).ConfigureAwait(true);
                nodes.Add(node);
            }

            FolderNodes.Clear();
            foreach (var node in nodes) {
                FolderNodes.Add(node);
                node.IsExpanded = true;
                await node.EnsureChildrenLoadedAsync();
            }

            PersistRoots();
            NotifyLibraryDisplay();

            if (selectFirst && FolderNodes.Count > 0) {
                _isInitializing = false;
                SelectedFolder  = FolderNodes[0];
            }
        }
        finally {
            IsLibraryLoading = false;
            _isInitializing  = false;
        }
    }

    private async Task AddRootsAsync(IReadOnlyList<string> paths, bool selectNew) {
        var existing = FolderNodes.Select(n => n.FullPath).ToList();
        var toAdd    = NormalizeNewRoots(paths, existing);
        FolderNode? lastAdded = null;

        foreach (var nested in FolderNodes.Where(n => toAdd.Any(p => IsAncestorOf(p, n.FullPath))).ToList())
            FolderNodes.Remove(nested);

        foreach (var path in toAdd) {
            var node = await Task.Run(() => FolderNode.FromPath(path)).ConfigureAwait(true);
            FolderNodes.Add(node);
            node.IsExpanded = true;
            await node.EnsureChildrenLoadedAsync();
            lastAdded = node;
        }

        PersistRoots();
        NotifyLibraryDisplay();

        if (selectNew && lastAdded is not null) {
            _isInitializing = false;
            SelectedFolder  = lastAdded;
        }
    }

    private static List<string> NormalizeNewRoots(IEnumerable<string> paths, IReadOnlyList<string> existing) {
        var result = new List<string>();
        foreach (var raw in paths) {
            if (string.IsNullOrWhiteSpace(raw) || !Directory.Exists(raw))
                continue;
            var full = NormalizeDir(raw);
            if (existing.Any(e => SamePath(e, full) || IsAncestorOf(e, full)))
                continue;
            if (result.Any(e => SamePath(e, full) || IsAncestorOf(e, full)))
                continue;
            result.RemoveAll(e => IsAncestorOf(full, e));
            result.Add(full);
        }

        return result;
    }

    private void PersistRoots() {
        _settings.LibraryRoots = FolderNodes.Select(n => n.FullPath).ToList();
        _settings.LibraryRoot  = _settings.LibraryRoots.FirstOrDefault();
        _settings.Save();
    }

    private void NotifyLibraryDisplay() {
        OnPropertyChanged(nameof(HasLibraryRoot));
        OnPropertyChanged(nameof(LibraryRoot));
        OnPropertyChanged(nameof(LibraryRootDisplay));
        OnPropertyChanged(nameof(LibraryRootsTooltip));
        OnPropertyChanged(nameof(CanRemoveLibraryRoot));
    }

    partial void OnIncludeSubfoldersChanged(bool value) {
        _settings.IncludeSubfolders = value;
        _settings.Save();
        if (!string.IsNullOrEmpty(CurrentFolderPath))
            NotifyMusicFolderConsumers(CurrentFolderPath, force: true);
    }

    [RelayCommand]
    private void IndexLibrary() {
        if (FolderNodes.Count == 0)
            return;
        var host = ModuleHub.Find<ITrackListHost>();
        if (host is null)
            return;
        host.IndexLibrary(FolderNodes.Select(n => n.FullPath).ToList());
    }

    private void NotifyMusicFolderConsumers(string path, bool force = false, bool? includeSubfolders = null) {
        if (!force && string.Equals(_lastLoadedFolder, path, StringComparison.OrdinalIgnoreCase) && !_isInitializing)
            return;

        _lastLoadedFolder = path;
        var recursive = includeSubfolders ?? IncludeSubfolders;

        var extensions = MainWindowViewModel.LoadedExtensions;
        if (extensions is not null) {
            foreach (var consumer in extensions.OfType<IMusicFolderConsumer>())
                consumer.LoadMusicFolder(path, recursive);
        }

        if (!_isInitializing)
            RequestId3Tab?.Invoke(this, EventArgs.Empty);
    }

    private async Task<FolderNode?> ExpandToFolderAsync(FolderNode root, string targetFullPath) {
        var current = root;
        while (true) {
            if (SamePath(current.FullPath, targetFullPath))
                return current;

            current.IsExpanded = true;
            await current.EnsureChildrenLoadedAsync().ConfigureAwait(true);

            FolderNode? next = null;
            foreach (var child in current.Children) {
                if (child.IsPlaceholder)
                    continue;
                if (SamePath(child.FullPath, targetFullPath) || IsAncestorOf(child.FullPath, targetFullPath)) {
                    next = child;
                    break;
                }
            }

            if (next is null)
                return current;
            current = next;
        }
    }

    private FolderNode? RootOf(FolderNode? node) {
        if (node is null)
            return null;
        return FolderNodes.FirstOrDefault(r =>
            SamePath(r.FullPath, node.FullPath) || IsAncestorOf(r.FullPath, node.FullPath));
    }

    private static bool IsAncestorOf(string ancestor, string path) {
        var a = NormalizeDir(ancestor) + Path.DirectorySeparatorChar;
        var p = NormalizeDir(path) + Path.DirectorySeparatorChar;
        return p.StartsWith(a, StringComparison.OrdinalIgnoreCase);
    }

    private static bool SamePath(string left, string right) =>
        string.Equals(NormalizeDir(left), NormalizeDir(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeDir(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
