using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Buran.ID3Editor.Models;
using Buran.ID3Editor.Services;
using Buran.Localization;
using Buran.SQLite;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.ID3Editor.ViewModels;

public partial class ImportCatalogSuggestionsViewModel : ObservableObject {
    private readonly Dictionary<string, CatalogSuggestionItem> _parked = new(StringComparer.OrdinalIgnoreCase);

    public ImportCatalogSuggestionsViewModel(IReadOnlyList<CatalogSuggestionItem> suggestions) {
        Artists = new ObservableCollection<CatalogSuggestionItem>(
            suggestions.Where(s => s.IsArtist));
        Genres = new ObservableCollection<CatalogSuggestionItem>(
            suggestions.Where(s => s.IsGenre));
        Moods = new ObservableCollection<CatalogSuggestionItem>(
            suggestions.Where(s => s.IsMood));
        ExistingArtists = new ObservableCollection<DatabaseTable_ArtistNames>(
            DBConnector.LoadTableContent_ArtistNames()
                .OrderBy(a => a.PreferredArtistName, StringComparer.CurrentCultureIgnoreCase));
        BlockedValues = new ObservableCollection<DatabaseTable_BlockedCatalogValues>(
            DBConnector.LoadTableContent_BlockedCatalogValues());

        foreach (var item in AllItems())
            item.PropertyChanged += OnItemPropertyChanged;

        L.WhenChanged(() => {
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(ApplyBlockReason));
            OnPropertyChanged(nameof(HasApplyBlockReason));
            foreach (var blocked in BlockedValues)
                blocked.NotifyKindLabel();
        });
    }

    public event EventHandler? CloseRequested;

    public ObservableCollection<CatalogSuggestionItem> Artists { get; }
    public ObservableCollection<CatalogSuggestionItem> Genres  { get; }
    public ObservableCollection<CatalogSuggestionItem> Moods   { get; }
    public ObservableCollection<DatabaseTable_ArtistNames> ExistingArtists { get; }
    public ObservableCollection<DatabaseTable_BlockedCatalogValues> BlockedValues { get; }

    public bool HasArtists => Artists.Count > 0;
    public bool HasGenres  => Genres.Count  > 0;
    public bool HasMoods   => Moods.Count   > 0;
    public bool HasBlocked => BlockedValues.Count > 0;

    public string Summary {
        get {
            var parts = new[] {
                Artists.Count > 0 ? L.Format("Catalog.SummaryArtists", Artists.Count) : null,
                Genres.Count  > 0 ? L.Format("Catalog.SummaryGenres", Genres.Count) : null,
                Moods.Count   > 0 ? L.Format("Catalog.SummaryMoods", Moods.Count) : null
            };
            return string.Join(", ", parts.Where(p => p is not null));
        }
    }

    public bool CanApply {
        get {
            var included = AllItems().Where(i => i.Include).ToList();
            if (included.Count == 0)
                return false;

            return included
                .Where(i => i.IsArtist && i.IsAlternativeName)
                .All(i => i.HasAlternativeTarget);
        }
    }

    public bool HasApplyBlockReason => ApplyBlockReason.Length > 0;

    public string ApplyBlockReason {
        get {
            if (!AllItems().Any(i => i.Include))
                return L.Get("Catalog.SelectAtLeastOne");

            if (AllItems().Any(i => i.Include && i.IsArtist && i.IsAlternativeName && !i.HasAlternativeTarget))
                return L.Get("Catalog.NeedPreferredForAlt");

            return string.Empty;
        }
    }

    [RelayCommand]
    private void Apply() {
        if (!CanApply)
            return;

        foreach (var item in Artists.Where(i => i.Include)) {
            if (item.IsAlternativeName) {
                var preferredId = ResolvePreferredArtistId(item);
                if (preferredId is int id)
                    DBConnector.InsertAlternativeArtistName(item.Value, id);
            }
            else {
                DBConnector.InsertArtistName(item.Value, "");
            }
        }

        foreach (var item in Genres.Where(i => i.Include)) {
            if (item.IsAlternativeName && item.RefersToGenre is { } genre)
                DBConnector.InsertAlternativeGenreName(item.Value, genre.ID);
            else
                DBConnector.InsertGenreName(item.Value);
        }

        foreach (var item in Moods.Where(i => i.Include)) {
            if (item.IsAlternativeName && item.RefersToMood is { } mood)
                DBConnector.InsertAlternativeMoodName(item.Value, mood.ID);
            else
                DBConnector.InsertMoodName(item.Value);
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Skip() => CloseRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void BlockValue(CatalogSuggestionItem? item) {
        if (item is null)
            return;

        var kind = CatalogSuggestionCollector.KindCode(item.Kind);
        _parked[ParkKey(kind, item.Value)] = item;
        DBConnector.InsertBlockedCatalogValue(kind, item.Value);
        RemoveSuggestion(item);
        ReloadBlockedValues();
        RaiseListState();
    }

    [RelayCommand]
    private void UnblockValue(DatabaseTable_BlockedCatalogValues? item) {
        if (item is null)
            return;

        var kind  = item.Kind;
        var value = item.Value;
        DBConnector.DeleteBlockedCatalogValue(item.Id);

        if (_parked.Remove(ParkKey(kind, value), out var parked))
            RestoreSuggestion(parked);
        else
            RestoreSuggestion(CreateSuggestion(kind, value));

        ReloadBlockedValues();
        RaiseListState();
    }

    [RelayCommand]
    private void CreatePreferredFor(CatalogSuggestionItem? item) {
        if (item is null || !item.HasNewPreferredName)
            return;

        var id = EnsurePreferredArtist(item.NewPreferredName, item.NewRealName);
        ReloadExistingArtists();
        item.RefersToArtist   = ExistingArtists.FirstOrDefault(a => a.ID == id);
        item.NewPreferredName = "";
        item.NewRealName      = "";
    }

    private int? ResolvePreferredArtistId(CatalogSuggestionItem item) {
        if (item.HasNewPreferredName)
            return EnsurePreferredArtist(item.NewPreferredName, item.NewRealName);
        return item.RefersToArtist?.ID;
    }

    private int EnsurePreferredArtist(string preferredName, string? realName) {
        var name     = preferredName.Trim();
        var resolved = DBConnector.CheckForPreferredName(name);
        if (resolved.ArtistNameStatus != ArtistNameStatus.IsNonExistent) {
            var existing = DBConnector.LoadTableContent_ArtistNames()
                .First(a => a.PreferredArtistName.Equals(resolved.PreferredArtistName, StringComparison.OrdinalIgnoreCase));
            return existing.ID;
        }

        return DBConnector.InsertArtistName(name, realName?.Trim() ?? "");
    }

    private void ReloadExistingArtists() {
        ExistingArtists.Clear();
        foreach (var artist in DBConnector.LoadTableContent_ArtistNames()
                     .OrderBy(a => a.PreferredArtistName, StringComparer.CurrentCultureIgnoreCase))
            ExistingArtists.Add(artist);
    }

    private IEnumerable<CatalogSuggestionItem> AllItems() =>
        Artists.Concat(Genres).Concat(Moods);

    private void RemoveSuggestion(CatalogSuggestionItem item) {
        item.PropertyChanged -= OnItemPropertyChanged;
        if (item.IsArtist)
            Artists.Remove(item);
        else if (item.IsGenre)
            Genres.Remove(item);
        else
            Moods.Remove(item);
    }

    private void RestoreSuggestion(CatalogSuggestionItem item) {
        if (item is null || IsAlreadyInCatalog(item) || AlreadyListed(item))
            return;

        item.Include = true;
        item.PropertyChanged -= OnItemPropertyChanged;
        item.PropertyChanged += OnItemPropertyChanged;
        InsertSorted(ListFor(item.Kind), item);
    }

    private CatalogSuggestionItem CreateSuggestion(string kind, string value) {
        var suggestionKind = kind.Trim().ToUpperInvariant() switch {
            DatabaseTable_BlockedCatalogValues.KindGenre => CatalogSuggestionKind.Genre,
            DatabaseTable_BlockedCatalogValues.KindMood  => CatalogSuggestionKind.Mood,
            _                                            => CatalogSuggestionKind.Artist
        };

        var existingArtists = ExistingArtists.ToList();
        var existingGenres = DBConnector.LoadTableContent_GenreNames()
            .OrderBy(g => g.GenreName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var existingMoods = DBConnector.LoadTableContent_MoodNames()
            .OrderBy(m => m.MoodName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new CatalogSuggestionItem(suggestionKind, value.Trim(), L.Get("Catalog.SourceUnblocked")) {
            ExistingArtists = existingArtists,
            ExistingGenres  = existingGenres,
            ExistingMoods   = existingMoods
        };
    }

    private ObservableCollection<CatalogSuggestionItem> ListFor(CatalogSuggestionKind kind) => kind switch {
        CatalogSuggestionKind.Artist => Artists,
        CatalogSuggestionKind.Genre  => Genres,
        _                            => Moods
    };

    private bool AlreadyListed(CatalogSuggestionItem item) =>
        ListFor(item.Kind).Any(existing =>
            existing.Value.Equals(item.Value, StringComparison.OrdinalIgnoreCase));

    private static bool IsAlreadyInCatalog(CatalogSuggestionItem item) => item.Kind switch {
        CatalogSuggestionKind.Artist => DBConnector.ResolveArtistName(item.Value).IsKnown,
        CatalogSuggestionKind.Genre  => DBConnector.ResolveGenreName(item.Value).IsKnown,
        CatalogSuggestionKind.Mood   => DBConnector.ResolveMoodName(item.Value).IsKnown,
        _                            => false
    };

    private static void InsertSorted(
        ObservableCollection<CatalogSuggestionItem> list,
        CatalogSuggestionItem item) {
        var index = 0;
        while (index < list.Count &&
               string.Compare(list[index].Value, item.Value, StringComparison.CurrentCultureIgnoreCase) < 0)
            index++;
        list.Insert(index, item);
    }

    private static string ParkKey(string kind, string value) =>
        $"{kind.Trim().ToUpperInvariant()}\u001f{value.Trim()}";

    private void ReloadBlockedValues() {
        BlockedValues.Clear();
        foreach (var row in DBConnector.LoadTableContent_BlockedCatalogValues())
            BlockedValues.Add(row);
    }

    private void RaiseListState() {
        OnPropertyChanged(nameof(HasArtists));
        OnPropertyChanged(nameof(HasGenres));
        OnPropertyChanged(nameof(HasMoods));
        OnPropertyChanged(nameof(HasBlocked));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(CanApply));
        OnPropertyChanged(nameof(ApplyBlockReason));
        OnPropertyChanged(nameof(HasApplyBlockReason));
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName is nameof(CatalogSuggestionItem.Include)
            or nameof(CatalogSuggestionItem.ArtistMode)
            or nameof(CatalogSuggestionItem.RefersToArtist)
            or nameof(CatalogSuggestionItem.IsAlternativeName)
            or nameof(CatalogSuggestionItem.NewPreferredName)
            or nameof(CatalogSuggestionItem.HasAlternativeTarget)
            or nameof(CatalogSuggestionItem.RefersToGenre)
            or nameof(CatalogSuggestionItem.RefersToMood)) {
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(ApplyBlockReason));
            OnPropertyChanged(nameof(HasApplyBlockReason));
        }
    }
}
