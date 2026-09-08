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
        CurrentTags   = new ObservableCollection<BulkTagItem>();
        KnownTags     = new ObservableCollection<string>();
        PreviewItems  = new ObservableCollection<TagPreviewItem>();

        ReloadKnownTags();
        CurrentTags.CollectionChanged += (_, _) => RefreshAfterListChange();
        LoadFromSelection();
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
    public ObservableCollection<BulkTagItem>   CurrentTags   { get; }
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

    public bool CanAddTag {
        get {
            if (string.IsNullOrWhiteSpace(NewTagInput))
                return false;

            var name     = ResolveName(NewTagInput.Trim());
            var existing = FindTag(name);
            if (existing?.ApplyToAll == true)
                return false;
            return existing is null || !IsOnAllFiles(name);
        }
    }

    public bool CanApply => _files.Count > 0 && HasDelta();

    partial void OnNewTagInputChanged(string value) => OnPropertyChanged(nameof(CanAddTag));

    [RelayCommand]
    private void AddTag() {
        if (!CanAddTag)
            return;

        var name     = PersistAndResolve(NewTagInput.Trim());
        var existing = FindTag(name);
        if (existing is null)
            CurrentTags.Add(new BulkTagItem(name, applyToAll: true));
        else
            existing.ApplyToAll = true;

        NewTagInput = "";
        RefreshAfterListChange();
    }

    [RelayCommand]
    private void RemoveTag(object? tag) {
        var name = tag switch {
            BulkTagItem item => item.Name,
            string text      => text,
            _                => tag?.ToString()
        };
        if (string.IsNullOrWhiteSpace(name))
            return;

        var match = CurrentTags.FirstOrDefault(t =>
            t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
            CurrentTags.Remove(match);

        RefreshAfterListChange();
    }

    [RelayCommand]
    private void ClearTags() {
        CurrentTags.Clear();
        RefreshAfterListChange();
    }

    [RelayCommand]
    private void LoadFromSelection() {
        CurrentTags.Clear();
        foreach (var file in _files) {
            foreach (var tag in ReadTags(file)) {
                var name = tag.Trim();
                if (name.Length > 0 && !HasTag(name))
                    CurrentTags.Add(new BulkTagItem(name));
            }
        }

        _baselineTags = new HashSet<string>(CurrentTagNames(), StringComparer.OrdinalIgnoreCase);
        RefreshAfterListChange();
    }

    [RelayCommand]
    private void Apply() {
        if (!CanApply)
            return;

        GetDelta(out var toAdd, out var toRemove);
        toAdd = toAdd.Select(PersistAndResolve).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var file in _files)
            WriteTags(file, MergeTags(ReadTags(file), toAdd, toRemove));

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private void RefreshAfterListChange() {
        UpdatePreview();
        OnPropertyChanged(nameof(CanAddTag));
        OnPropertyChanged(nameof(CanApply));
        ApplyCommand.NotifyCanExecuteChanged();
        AddTagCommand.NotifyCanExecuteChanged();
    }

    private bool HasTag(string name) => FindTag(name) is not null;

    private BulkTagItem? FindTag(string name) =>
        CurrentTags.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private bool IsOnAllFiles(string name) =>
        _files.Count > 0 &&
        _files.All(f => ReadTags(f).Any(t => t.Trim().Equals(name, StringComparison.OrdinalIgnoreCase)));

    private IEnumerable<string> CurrentTagNames() => CurrentTags.Select(t => t.Name);

    private bool HasDelta() {
        GetDelta(out var toAdd, out var toRemove);
        return toAdd.Count > 0 || toRemove.Count > 0;
    }

    private void GetDelta(out List<string> toAdd, out List<string> toRemove) {
        var currentSet = new HashSet<string>(CurrentTagNames(), StringComparer.OrdinalIgnoreCase);
        toRemove = _baselineTags.Where(t => !currentSet.Contains(t)).ToList();
        toAdd = CurrentTags
            .Where(t => t.ApplyToAll || !_baselineTags.Contains(t.Name))
            .Select(t => t.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

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
                if (resolved.ArtistNameStatus == ArtistNameStatus.IsNonExistent &&
                    !DBConnector.IsCatalogValueBlocked(DatabaseTable_BlockedCatalogValues.KindArtist, resolved.PreferredArtistName))
                    DBConnector.InsertArtistName(resolved.PreferredArtistName, "");
                name = resolved.PreferredArtistName;
                break;
            }
            case CatalogSuggestionKind.Genre: {
                var resolved = DBConnector.ResolveGenreName(name);
                name = resolved.PreferredName;
                if (!resolved.IsKnown &&
                    !DBConnector.IsCatalogValueBlocked(DatabaseTable_BlockedCatalogValues.KindGenre, name))
                    DBConnector.InsertGenreName(name);
                break;
            }
            default: {
                var resolved = DBConnector.ResolveMoodName(name);
                name = resolved.PreferredName;
                if (!resolved.IsKnown &&
                    !DBConnector.IsCatalogValueBlocked(DatabaseTable_BlockedCatalogValues.KindMood, name))
                    DBConnector.InsertMoodName(name);
                break;
            }
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
            .Select(t => t.Trim())
            .Where(t => t.Length > 0 &&
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
        GetDelta(out var toAdd, out var toRemove);

        PreviewItems.Clear();
        foreach (var file in _files) {
            var oldTags = ReadTags(file)
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .ToList();
            var newTags = MergeTags(oldTags, toAdd, toRemove);
            PreviewItems.Add(new TagPreviewItem {
                FileName = file.FileName,
                OldTags  = oldTags.Count > 0 ? string.Join(", ", oldTags) : L.Get("Common.None"),
                NewTags  = newTags.Count > 0 ? string.Join(", ", newTags) : L.Get("Common.None")
            });
        }
    }
}
