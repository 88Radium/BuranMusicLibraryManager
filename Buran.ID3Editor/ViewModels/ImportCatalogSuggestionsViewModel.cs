using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Buran.ID3Editor.Models;
using Buran.Localization;
using Buran.SQLite;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.ID3Editor.ViewModels;

public partial class ImportCatalogSuggestionsViewModel : ObservableObject {
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

        foreach (var item in AllItems())
            item.PropertyChanged += OnItemPropertyChanged;

        L.WhenChanged(() => {
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(ApplyBlockReason));
            OnPropertyChanged(nameof(HasApplyBlockReason));
        });
    }

    public event EventHandler? CloseRequested;

    public ObservableCollection<CatalogSuggestionItem> Artists { get; }
    public ObservableCollection<CatalogSuggestionItem> Genres  { get; }
    public ObservableCollection<CatalogSuggestionItem> Moods   { get; }
    public ObservableCollection<DatabaseTable_ArtistNames> ExistingArtists { get; }

    public bool HasArtists => Artists.Count > 0;
    public bool HasGenres  => Genres.Count  > 0;
    public bool HasMoods   => Moods.Count   > 0;

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

        foreach (var item in Genres.Where(i => i.Include))
            DBConnector.InsertGenreName(item.Value);

        foreach (var item in Moods.Where(i => i.Include))
            DBConnector.InsertMoodName(item.Value);

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Skip() => CloseRequested?.Invoke(this, EventArgs.Empty);

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

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName is nameof(CatalogSuggestionItem.Include)
            or nameof(CatalogSuggestionItem.ArtistMode)
            or nameof(CatalogSuggestionItem.RefersToArtist)
            or nameof(CatalogSuggestionItem.IsAlternativeName)
            or nameof(CatalogSuggestionItem.NewPreferredName)
            or nameof(CatalogSuggestionItem.HasAlternativeTarget)) {
            OnPropertyChanged(nameof(CanApply));
            OnPropertyChanged(nameof(ApplyBlockReason));
            OnPropertyChanged(nameof(HasApplyBlockReason));
        }
    }
}
