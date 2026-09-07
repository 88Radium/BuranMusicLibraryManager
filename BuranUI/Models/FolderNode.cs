using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuranUI.Models;

public partial class FolderNode : ObservableObject {
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _fullPath = string.Empty;
    [ObservableProperty] private bool _isExpanded;
    [ObservableProperty] private bool _hasAudioFiles;
    [ObservableProperty] private bool _isPlaceholder;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<FolderNode> Children { get; } = new();

    private bool _childrenLoaded;

    partial void OnIsExpandedChanged(bool value) {
        if (value)
            _ = EnsureChildrenLoadedAsync();
    }

    public static FolderNode FromPath(string path) {
        var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var name = Path.GetFileName(trimmed);
        if (string.IsNullOrEmpty(name))
            name = path;

        var node = new FolderNode {
            Name     = name,
            FullPath = path,
        };

        try {
            node.HasAudioFiles = DirectoryHasAudio(path);
            if (HasVisibleSubdirectories(path))
                node.Children.Add(CreatePlaceholder());
        }
        catch {
            // Kein Zugriff: Knoten trotzdem anzeigen, ohne Kinder.
        }

        return node;
    }

    public static FolderNode CreatePlaceholder() => new() {
        Name          = "Laden …",
        IsPlaceholder = true,
    };

    public async Task EnsureChildrenLoadedAsync() {
        if (_childrenLoaded || IsPlaceholder)
            return;

        _childrenLoaded = true;
        IsLoading       = true;

        List<FolderNode> loaded;
        try {
            var path = FullPath;
            loaded = await Task.Run(() => {
                try {
                    return Directory.EnumerateDirectories(path)
                        .Where(IsVisibleDirectory)
                        .OrderBy(d => Path.GetFileName(d), StringComparer.CurrentCultureIgnoreCase)
                        .Select(FromPath)
                        .ToList();
                }
                catch {
                    return new List<FolderNode>();
                }
            }).ConfigureAwait(true);
        }
        finally {
            IsLoading = false;
        }

        Children.Clear();
        foreach (var child in loaded)
            Children.Add(child);
    }

    private static bool IsVisibleDirectory(string path) {
        var name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(name) || name[0] == '.')
            return false;
        if (name is "$RECYCLE.BIN" or "System Volume Information" or "lost+found")
            return false;

        try {
            var attrs = File.GetAttributes(path);
            if ((attrs & FileAttributes.Hidden) != 0 || (attrs & FileAttributes.System) != 0)
                return false;
        }
        catch {
            return false;
        }

        return true;
    }

    private static bool HasVisibleSubdirectories(string path) {
        try {
            return Directory.EnumerateDirectories(path).Any(IsVisibleDirectory);
        }
        catch {
            return false;
        }
    }

    private static bool DirectoryHasAudio(string path) {
        try {
            if (Directory.EnumerateFiles(path, "*.mp3").Any())
                return true;
            if (Directory.EnumerateFiles(path, "*.flac").Any())
                return true;
        }
        catch {
            // ignore
        }

        return false;
    }
}
