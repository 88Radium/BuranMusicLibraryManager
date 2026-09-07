using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Buran.ID3Editor.Models;
using Buran.ID3Editor.Services;
using Buran.ID3Editor.Types;
using Buran.SQLite;
using Buran.Types;
using Buran.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.ID3Editor.ViewModels;

public partial class BulkAddTagsViewModel : ObservableObject {
    private readonly List<Mp3FileObject> _files;
    private HashSet<string> _baselineTags = new(StringComparer.OrdinalIgnoreCase);

    public BulkAddTagsViewModel(IEnumerable<Mp3FileObject> files, CatalogSuggestionKind kind) {
        Kind          = kind;
        _files        = files.ToList();
        SelectedFiles = new ObservableCollection<Mp3FileObject>(_files);
        CurrentTags   = new ObservableCollection<string>();
        KnownTags     = new ObservableCollection<string>();
        PreviewItems  = new ObservableCollection<TagPreviewItem>();

        ReloadKnownTags();
        LoadFromSelection();
        CurrentTags.CollectionChanged += (_, _) => {
            UpdatePreview();
            OnPropertyChanged(nameof(CanApply));
        };
        UpdatePreview();
        L.WhenChanged(() => {
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(ListHeader));
            OnPropertyChanged(nameof(InputHeader));
            OnPropertyChanged(nameof(InputWatermark));
            UpdatePreview();
        });
    }

    public event EventHandler? CloseRequested;

    public CatalogSuggestionKind Kind { get; }

    public ObservableCollection<Mp3FileObject> SelectedFiles { get; }
    public ObservableCollection<string>        CurrentTags   { get; }
    public ObservableCollection<string>        KnownTags     { get; }
    public ObservableCollection<TagPreviewItem> PreviewItems { get; }

    public string WindowTitle => Kind switch {
        CatalogSuggestionKind.Artist => L.Get("Bulk.AddArtistsTitle"),
        CatalogSuggestionKind.Genre  => L.Get("Bulk.AddGenresTitle"),
        _                            => L.Get("Bulk.AddMoodsTitle")
    };

    public string ListHeader => Kind switch {
        CatalogSuggestionKind.Artist => L.Get("Bulk.ArtistsInSelection"),
        CatalogSuggestionKind.Genre  => L.Get("Bulk.GenresInSelection"),
        _                            => L.Get("Bulk.MoodsInSelection")
    };

    public string InputHeader => Kind switch {
        CatalogSuggestionKind.Artist => L.Get("Bulk.AddArtist"),
        CatalogSuggestionKind.Genre  => L.Get("Bulk.AddGenre"),
        _                            => L.Get("Bulk.AddMood")
    };

    public string InputWatermark => Kind switch {
        CatalogSuggestionKind.Artist => L.Get("Bulk.ArtistName"),
        CatalogSuggestionKind.Genre  => L.Get("Common.Genre"),
        _                            => L.Get("Common.Mood")
    };

    [ObservableProperty] private string _newTagInput = "";

    public bool CanAddTag =>
        !string.IsNullOrWhiteSpace(NewTagInput) &&
        !CurrentTags.Contains(ResolveName(NewTagInput.Trim()), StringComparer.OrdinalIgnoreCase);

    public bool CanApply =>
        _files.Count > 0 &&
        !_baselineTags.SetEquals(CurrentTags);

    partial void OnNewTagInputChanged(string value) => OnPropertyChanged(nameof(CanAddTag));

    [RelayCommand]
    private void AddTag() {
        if (!CanAddTag)
            return;

        var name = PersistAndResolve(NewTagInput.Trim());
        if (!CurrentTags.Contains(name, StringComparer.OrdinalIgnoreCase))
            CurrentTags.Add(name);

        NewTagInput = "";
        OnPropertyChanged(nameof(CanAddTag));
    }

    [RelayCommand]
    private void RemoveTag(string? tag) {
        if (!string.IsNullOrWhiteSpace(tag))
            CurrentTags.Remove(tag);
    }

    [RelayCommand]
    private void ClearTags() => CurrentTags.Clear();

    [RelayCommand]
    private void LoadFromSelection() {
        CurrentTags.Clear();
        foreach (var file in _files) {
            foreach (var tag in ReadTags(file)) {
                if (!string.IsNullOrWhiteSpace(tag) &&
                    !CurrentTags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    CurrentTags.Add(tag);
            }
        }

        _baselineTags = new HashSet<string>(CurrentTags, StringComparer.OrdinalIgnoreCase);
        OnPropertyChanged(nameof(CanApply));
    }

    [RelayCommand]
    private void Apply() {
        if (!CanApply)
            return;

        var current = CurrentTags
            .Select(PersistAndResolve)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var currentSet = new HashSet<string>(current, StringComparer.OrdinalIgnoreCase);
        var toAdd = current.Where(t => !_baselineTags.Contains(t)).ToList();
        var toRemove = _baselineTags.Where(t => !currentSet.Contains(t)).ToList();

        foreach (var file in _files)
            WriteTags(file, MergeTags(ReadTags(file), toAdd, toRemove));

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void ReloadKnownTags() {
        KnownTags.Clear();
        IEnumerable<string> source = Kind switch {
            CatalogSuggestionKind.Artist => CatalogNameCache.LoadArtistSuggestionNames(),
            CatalogSuggestionKind.Genre  => CatalogNameCache.LoadGenreSuggestionNames(),
            _                            => CatalogNameCache.LoadMoodSuggestionNames()
        };
        foreach (var name in source)
            KnownTags.Add(name);
    }

    private string PersistAndResolve(string raw) {
        var name = ResolveName(raw);
        switch (Kind) {
            case CatalogSuggestionKind.Artist: {
                var resolved = DBConnector.CheckForPreferredName(name);
                if (resolved.ArtistNameStatus == ArtistNameStatus.IsNonExistent)
                    DBConnector.InsertArtistName(resolved.PreferredArtistName, "");
                name = resolved.PreferredArtistName;
                break;
            }
            case CatalogSuggestionKind.Genre:
                if (!KnownTags.Any(g => g.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    DBConnector.InsertGenreName(name);
                break;
            default:
                if (!KnownTags.Any(m => m.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    DBConnector.InsertMoodName(name);
                break;
        }

        ReloadKnownTags();
        return name;
    }

    private string ResolveName(string raw) {
        var existing = KnownTags.FirstOrDefault(v => v.Equals(raw, StringComparison.OrdinalIgnoreCase));
        return existing ?? raw;
    }

    private IEnumerable<string> ReadTags(Mp3FileObject file) => Kind switch {
        CatalogSuggestionKind.Artist => file.Id3ArtistCollection ?? [],
        CatalogSuggestionKind.Genre  => file.Id3GenreCollection ?? [],
        _                            => file.Id3MoodCollection ?? []
    };

    private static List<string> MergeTags(
        IEnumerable<string> existing,
        IReadOnlyList<string> toAdd,
        IReadOnlyList<string> toRemove) {
        var next = existing
            .Where(t => !string.IsNullOrWhiteSpace(t) &&
                        toRemove.All(r => !t.Equals(r, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        foreach (var tag in toAdd) {
            if (!next.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)))
                next.Add(tag);
        }

        return next;
    }

    private void WriteTags(Mp3FileObject file, List<string> tags) {
        var list = new ObservableCollection<string>(tags);
        switch (Kind) {
            case CatalogSuggestionKind.Artist:
                file.Id3ArtistCollection = list;
                break;
            case CatalogSuggestionKind.Genre:
                file.Id3GenreCollection = list;
                break;
            default:
                file.Id3MoodCollection = list;
                break;
        }
    }

    private void UpdatePreview() {
        var currentSet = new HashSet<string>(CurrentTags, StringComparer.OrdinalIgnoreCase);
        var toAdd      = CurrentTags.Where(t => !_baselineTags.Contains(t)).ToList();
        var toRemove   = _baselineTags.Where(t => !currentSet.Contains(t)).ToList();

        PreviewItems.Clear();
        foreach (var file in _files) {
            var oldTags = ReadTags(file).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            var newTags = MergeTags(oldTags, toAdd, toRemove);
            PreviewItems.Add(new TagPreviewItem {
                FileName = file.FileName,
                OldTags  = oldTags.Count > 0 ? string.Join(", ", oldTags) : L.Get("Common.None"),
                NewTags  = newTags.Count > 0 ? string.Join(", ", newTags) : L.Get("Common.None")
            });
        }
    }
}
