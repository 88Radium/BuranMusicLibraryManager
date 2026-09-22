using System;
using System.Collections.Generic;
using System.IO;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Buran.DBEditor.Models;
using Buran.Interfaces;
using Buran.Localization;
using Buran.SQLite;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.DBEditor.ViewModels;

public partial class DBEditorTabViewModel : ObservableObject {
    [ObservableProperty] private DatabaseTable_ArtistNames? _selectedArtistName;
    [ObservableProperty] private DatabaseTable_AlternativeArtistNameVariants? _selectedAlternativeName;
    [ObservableProperty] private DatabaseTable_ArtistNames? _selectedMember;
    [ObservableProperty] private DatabaseTable_ArtistNames? _selectedGroup;
    [ObservableProperty] private AssignableTag? _selectedGenre;
    [ObservableProperty] private AssignableTag? _selectedMood;
    [ObservableProperty] private DatabaseTable_AlternativeGenreNameVariants? _selectedGenreVariant;
    [ObservableProperty] private DatabaseTable_AlternativeMoodNameVariants? _selectedMoodVariant;
    [ObservableProperty] private AssignableTag? _genreFilter;
    [ObservableProperty] private AssignableTag? _moodFilter;

    [ObservableProperty] private ObservableCollection<DatabaseTable_ArtistNames> _artistNames = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_ArtistNames> _filteredArtistNames = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_AlternativeArtistNameVariants> _alternativeArtistNameVariants = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_AlternativeArtistNameVariants> _filteredAlternativeArtistNameVariants = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_ArtistNames> _membersOfSelected = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_ArtistNames> _groupsOfSelected = [];
    [ObservableProperty] private ObservableCollection<AssignableTag> _genres = [];
    [ObservableProperty] private ObservableCollection<AssignableTag> _moods = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_AlternativeGenreNameVariants> _genreVariants = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_AlternativeGenreNameVariants> _filteredGenreVariants = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_AlternativeMoodNameVariants> _moodVariants = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_AlternativeMoodNameVariants> _filteredMoodVariants = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_FeatureKeywords> _featureKeywords = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_FeatureKeywords> _filteredFeatureKeywords = [];
    [ObservableProperty] private DatabaseTable_FeatureKeywords? _selectedFeatureKeyword;
    [ObservableProperty] private DatabaseTable_ArtistNames? _selectedArtistForNewAlternative;
    [ObservableProperty] private AssignableTag? _selectedGenreForNewAlternative;
    [ObservableProperty] private AssignableTag? _selectedMoodForNewAlternative;
    [ObservableProperty] private CatalogInsertMode _artistInsertMode = CatalogInsertMode.Preferred;
    [ObservableProperty] private CatalogInsertMode _genreInsertMode = CatalogInsertMode.Preferred;
    [ObservableProperty] private CatalogInsertMode _moodInsertMode = CatalogInsertMode.Preferred;

    [ObservableProperty] private ObservableCollection<DatabaseTable_BlockedCatalogValues> _blockedArtists = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_BlockedCatalogValues> _blockedGenres = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_BlockedCatalogValues> _blockedMoods = [];

    [ObservableProperty] private string _newPreferredArtistName = "";
    [ObservableProperty] private string _newRealName = "";
    [ObservableProperty] private string _newAlternativeName = "";
    [ObservableProperty] private string _newMemberName = "";
    [ObservableProperty] private string _newGroupName = "";
    [ObservableProperty] private string _newGenreName = "";
    [ObservableProperty] private string _newMoodName = "";
    [ObservableProperty] private string _newGenreVariant = "";
    [ObservableProperty] private string _newMoodVariant = "";
    [ObservableProperty] private string _newKeyword = "";
    [ObservableProperty] private string _artistSearchText = "";

    public static readonly KeywordTypeOption[] KeywordTypes = [
        new(CollaborationMarkers.TypeCollaboration),
        new(CollaborationMarkers.TypeVersion)
    ];

    [ObservableProperty] private KeywordTypeOption _selectedKeywordType = KeywordTypes[0];

    private int _catalogReloadQueued;

    public bool HasSelectedArtist => SelectedArtistName is not null;
    public bool HasSelectedGenre  => SelectedGenre is not null;
    public bool HasSelectedMood   => SelectedMood is not null;
    public bool HasBlockedArtists => BlockedArtists.Count > 0;
    public bool HasBlockedGenres  => BlockedGenres.Count  > 0;
    public bool HasBlockedMoods   => BlockedMoods.Count   > 0;

    public bool HasSearchText => !string.IsNullOrWhiteSpace(ArtistSearchText);

    public bool AddArtistAsPreferred {
        get => ArtistInsertMode == CatalogInsertMode.Preferred;
        set { if (value) ArtistInsertMode = CatalogInsertMode.Preferred; }
    }

    public bool AddArtistAsAlternative {
        get => ArtistInsertMode == CatalogInsertMode.Alternative;
        set { if (value) ArtistInsertMode = CatalogInsertMode.Alternative; }
    }

    public bool AddGenreAsPreferred {
        get => GenreInsertMode == CatalogInsertMode.Preferred;
        set { if (value) GenreInsertMode = CatalogInsertMode.Preferred; }
    }

    public bool AddGenreAsAlternative {
        get => GenreInsertMode == CatalogInsertMode.Alternative;
        set { if (value) GenreInsertMode = CatalogInsertMode.Alternative; }
    }

    public bool AddMoodAsPreferred {
        get => MoodInsertMode == CatalogInsertMode.Preferred;
        set { if (value) MoodInsertMode = CatalogInsertMode.Preferred; }
    }

    public bool AddMoodAsAlternative {
        get => MoodInsertMode == CatalogInsertMode.Alternative;
        set { if (value) MoodInsertMode = CatalogInsertMode.Alternative; }
    }

    public string AddArtistActionCaption =>
        AddArtistAsAlternative ? L.Get("Db.AddVariant") : L.Get("Db.AddArtist");

    public string AddGenreActionCaption =>
        AddGenreAsAlternative ? L.Get("Db.AddGenreVariant") : L.Get("Db.AddGenre");

    public string AddMoodActionCaption =>
        AddMoodAsAlternative ? L.Get("Db.AddMoodVariant") : L.Get("Db.AddMood");

    public string ArtistListCaption {
        get {
            var filters = new List<string>();
            if (GenreFilter is not null) filters.Add(L.Format("Db.GenreFilterPrefix", GenreFilter.Name));
            if (MoodFilter is not null) filters.Add(L.Format("Db.MoodFilterPrefix", MoodFilter.Name));
            if (HasSearchText) filters.Add(L.Format("Db.SearchFilterPrefix", ArtistSearchText.Trim()));
            return filters.Count == 0
                ? L.Get("Db.Artists")
                : L.Format("Db.ArtistsFiltered", string.Join(", ", filters));
        }
    }

    public string AlternativeListCaption =>
        SelectedArtistName is null
            ? L.Get("Db.AlternativeNamesAll")
            : L.Format("Db.AlternativeNamesOf", SelectedArtistName.PreferredArtistName);

    public string MembersCaption =>
        SelectedArtistName is null
            ? L.Get("Db.Members")
            : L.Format("Db.MembersOf", SelectedArtistName.PreferredArtistName);

    public string GroupsCaption =>
        SelectedArtistName is null
            ? L.Get("Db.Groups")
            : L.Format("Db.GroupsOf", SelectedArtistName.PreferredArtistName);

    public IReadOnlyList<string> MembershipNameSuggestions =>
        ArtistNames
            .Where(a => a.ID != SelectedArtistName?.ID)
            .Select(a => a.PreferredArtistName)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    public DBEditorTabViewModel() {
        try {
            LoadAll();
        }
        catch (Exception ex) {
            Debug.WriteLine("DBEditor: load failed: " + ex.Message);
        }

        DBConnector.CatalogChanged += OnCatalogChanged;
        L.WhenChanged(RaiseCaptions);
    }

    private void OnCatalogChanged(object? sender, EventArgs e) {
        if (Interlocked.Exchange(ref _catalogReloadQueued, 1) == 1)
            return;

        Dispatcher.UIThread.Post(() => {
            Interlocked.Exchange(ref _catalogReloadQueued, 0);
            try {
                LoadAll();
            }
            catch (Exception ex) {
                Debug.WriteLine("DBEditor: catalog reload failed: " + ex.Message);
            }
        });
    }

    public void LoadAll() {
        var artistId        = SelectedArtistName?.ID;
        var altId           = SelectedAlternativeName?.ID;
        var artistForAltId  = SelectedArtistForNewAlternative?.ID;
        var genreForAltId   = SelectedGenreForNewAlternative?.Id;
        var moodForAltId    = SelectedMoodForNewAlternative?.Id;

        UnsubscribeCatalog();

        ArtistNames = new ObservableCollection<DatabaseTable_ArtistNames>(
            DBConnector.LoadTableContent_ArtistNames().OrderBy(a => a.PreferredArtistName, StringComparer.CurrentCultureIgnoreCase));
        AlternativeArtistNameVariants = new ObservableCollection<DatabaseTable_AlternativeArtistNameVariants>(
            DBConnector.LoadTableContent_AlternativeArtistNameVariants().OrderBy(a => a.AlternativeArtistName, StringComparer.CurrentCultureIgnoreCase));

        foreach (var artist in ArtistNames)
            artist.PropertyChanged += OnArtistPropertyChanged;
        foreach (var variant in AlternativeArtistNameVariants)
            variant.PropertyChanged += OnAlternativePropertyChanged;

        RebuildTags();
        RebuildKeywords();
        RebuildLabelVariants();
        RebuildBlockedValues();
        SelectedArtistName               = ArtistNames.FirstOrDefault(a => a.ID == artistId);
        SelectedAlternativeName          = AlternativeArtistNameVariants.FirstOrDefault(a => a.ID == altId);
        SelectedArtistForNewAlternative  = ArtistNames.FirstOrDefault(a => a.ID == artistForAltId) ?? SelectedArtistName;
        SelectedGenreForNewAlternative   = Genres.FirstOrDefault(g => g.Id == genreForAltId) ?? SelectedGenre;
        SelectedMoodForNewAlternative    = Moods.FirstOrDefault(m => m.Id == moodForAltId) ?? SelectedMood;
        RefreshFilteredArtists();
        RefreshFilteredAlternatives();
        RefreshFilteredKeywords();
        RefreshFilteredGenreVariants();
        RefreshFilteredMoodVariants();
        RefreshAssignments();
        RefreshMemberships();
        RaiseCaptions();
    }

    private void RebuildBlockedValues() {
        var blocked = DBConnector.LoadTableContent_BlockedCatalogValues();
        BlockedArtists = new ObservableCollection<DatabaseTable_BlockedCatalogValues>(
            blocked.Where(b => b.Kind.Equals(DatabaseTable_BlockedCatalogValues.KindArtist, StringComparison.OrdinalIgnoreCase)));
        BlockedGenres = new ObservableCollection<DatabaseTable_BlockedCatalogValues>(
            blocked.Where(b => b.Kind.Equals(DatabaseTable_BlockedCatalogValues.KindGenre, StringComparison.OrdinalIgnoreCase)));
        BlockedMoods = new ObservableCollection<DatabaseTable_BlockedCatalogValues>(
            blocked.Where(b => b.Kind.Equals(DatabaseTable_BlockedCatalogValues.KindMood, StringComparison.OrdinalIgnoreCase)));
    }

    private void UnsubscribeCatalog() {
        foreach (var artist in ArtistNames)
            artist.PropertyChanged -= OnArtistPropertyChanged;
        foreach (var variant in AlternativeArtistNameVariants)
            variant.PropertyChanged -= OnAlternativePropertyChanged;
        foreach (var variant in GenreVariants)
            variant.PropertyChanged -= OnGenreVariantPropertyChanged;
        foreach (var variant in MoodVariants)
            variant.PropertyChanged -= OnMoodVariantPropertyChanged;
        foreach (var keyword in FeatureKeywords)
            keyword.PropertyChanged -= OnKeywordPropertyChanged;
    }

    private void UnsubscribeTags() {
        foreach (var tag in Genres)
            tag.PropertyChanged -= OnTagPropertyChanged;
        foreach (var tag in Moods)
            tag.PropertyChanged -= OnTagPropertyChanged;
    }

    private void RebuildTags() {
        UnsubscribeTags();

        var selectedGenreId = SelectedGenre?.Id;
        var selectedMoodId  = SelectedMood?.Id;
        var genreFilterId   = GenreFilter?.Id;
        var moodFilterId    = MoodFilter?.Id;
        var assignedGenres  = SelectedArtistName is null ? [] : DBConnector.LoadGenreIdsForArtist(SelectedArtistName.ID);
        var assignedMoods   = SelectedArtistName is null ? [] : DBConnector.LoadMoodIdsForArtist(SelectedArtistName.ID);

        Genres = new ObservableCollection<AssignableTag>(
            DBConnector.LoadTableContent_GenreNames()
                .Select(g => CreateTag(TagKind.Genre, g.ID, g.GenreName, assignedGenres.Contains(g.ID), genreFilterId == g.ID)));
        Moods = new ObservableCollection<AssignableTag>(
            DBConnector.LoadTableContent_MoodNames()
                .Select(m => CreateTag(TagKind.Mood, m.ID, m.MoodName, assignedMoods.Contains(m.ID), moodFilterId == m.ID)));

        SelectedGenre = Genres.FirstOrDefault(g => g.Id == selectedGenreId);
        SelectedMood  = Moods.FirstOrDefault(m => m.Id == selectedMoodId);
        GenreFilter   = Genres.FirstOrDefault(g => g.Id == genreFilterId);
        MoodFilter    = Moods.FirstOrDefault(m => m.Id == moodFilterId);
    }

    private AssignableTag CreateTag(TagKind kind, int id, string name, bool assigned, bool isFilter) {
        var tag = new AssignableTag(kind, id, name, assigned, OnTagAssignedChanged) {
            CanAssign      = HasSelectedArtist,
            IsFilterActive = isFilter
        };
        tag.PropertyChanged += OnTagPropertyChanged;
        return tag;
    }

    private void OnTagPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is not AssignableTag tag || e.PropertyName != nameof(AssignableTag.Name)) return;
        if (string.IsNullOrWhiteSpace(tag.Name)) return;

        if (tag.Kind == TagKind.Genre)
            DBConnector.UpdateGenreName(new DatabaseTable_GenreNames { ID = tag.Id, GenreName = tag.Name.Trim() });
        else
            DBConnector.UpdateMoodName(new DatabaseTable_MoodNames { ID = tag.Id, MoodName = tag.Name.Trim() });
        OnPropertyChanged(nameof(ArtistListCaption));
    }

    private void RefreshFilteredArtists() {
        IEnumerable<DatabaseTable_ArtistNames> query = ArtistNames;
        if (GenreFilter is not null) {
            var ids = DBConnector.LoadArtistIdsForGenre(GenreFilter.Id);
            query = query.Where(a => ids.Contains(a.ID));
        }
        if (MoodFilter is not null) {
            var ids = DBConnector.LoadArtistIdsForMood(MoodFilter.Id);
            query = query.Where(a => ids.Contains(a.ID));
        }

        var search = ArtistSearchText?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(a => ArtistMatchesSearch(a, search));

        FilteredArtistNames = new ObservableCollection<DatabaseTable_ArtistNames>(query);
        RefreshFilteredKeywords();
    }

    private void RebuildKeywords() {
        foreach (var keyword in FeatureKeywords)
            keyword.PropertyChanged -= OnKeywordPropertyChanged;

        var selectedId = SelectedFeatureKeyword?.Id;
        FeatureKeywords = new ObservableCollection<DatabaseTable_FeatureKeywords>(
            DBConnector.LoadTableContent_FeatureKeywords());
        foreach (var keyword in FeatureKeywords)
            keyword.PropertyChanged += OnKeywordPropertyChanged;

        SelectedFeatureKeyword = FeatureKeywords.FirstOrDefault(k => k.Id == selectedId);
    }

    private void RebuildLabelVariants() {
        foreach (var variant in GenreVariants)
            variant.PropertyChanged -= OnGenreVariantPropertyChanged;
        foreach (var variant in MoodVariants)
            variant.PropertyChanged -= OnMoodVariantPropertyChanged;

        var genreVariantId = SelectedGenreVariant?.Id;
        var moodVariantId  = SelectedMoodVariant?.Id;
        GenreVariants = new ObservableCollection<DatabaseTable_AlternativeGenreNameVariants>(
            DBConnector.LoadTableContent_AlternativeGenreNameVariants());
        MoodVariants = new ObservableCollection<DatabaseTable_AlternativeMoodNameVariants>(
            DBConnector.LoadTableContent_AlternativeMoodNameVariants());
        foreach (var variant in GenreVariants)
            variant.PropertyChanged += OnGenreVariantPropertyChanged;
        foreach (var variant in MoodVariants)
            variant.PropertyChanged += OnMoodVariantPropertyChanged;

        SelectedGenreVariant = GenreVariants.FirstOrDefault(v => v.Id == genreVariantId);
        SelectedMoodVariant  = MoodVariants.FirstOrDefault(v => v.Id == moodVariantId);
    }

    private void RefreshFilteredGenreVariants() {
        IEnumerable<DatabaseTable_AlternativeGenreNameVariants> query = GenreVariants;
        if (SelectedGenre is not null)
            query = query.Where(v => v.RefersToGenreName == SelectedGenre.Id);
        else
            query = [];
        FilteredGenreVariants = new ObservableCollection<DatabaseTable_AlternativeGenreNameVariants>(query);
    }

    private void RefreshFilteredMoodVariants() {
        IEnumerable<DatabaseTable_AlternativeMoodNameVariants> query = MoodVariants;
        if (SelectedMood is not null)
            query = query.Where(v => v.RefersToMoodName == SelectedMood.Id);
        else
            query = [];
        FilteredMoodVariants = new ObservableCollection<DatabaseTable_AlternativeMoodNameVariants>(query);
    }

    private void OnGenreVariantPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is DatabaseTable_AlternativeGenreNameVariants variant &&
            e.PropertyName == nameof(DatabaseTable_AlternativeGenreNameVariants.GenreNameVariant))
            DBConnector.UpdateAlternativeGenreName(variant);
    }

    private void OnMoodVariantPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is DatabaseTable_AlternativeMoodNameVariants variant &&
            e.PropertyName == nameof(DatabaseTable_AlternativeMoodNameVariants.MoodNameVariant))
            DBConnector.UpdateAlternativeMoodName(variant);
    }

    private void RefreshFilteredKeywords() {
        IEnumerable<DatabaseTable_FeatureKeywords> query = FeatureKeywords;
        var search = ArtistSearchText?.Trim();
        if (!string.IsNullOrEmpty(search)) {
            query = query.Where(k =>
                ContainsIgnoreCase(k.Keyword, search) ||
                ContainsIgnoreCase(k.TypeLabel, search));
        }

        FilteredFeatureKeywords = new ObservableCollection<DatabaseTable_FeatureKeywords>(query);
    }

    private void OnKeywordPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is not DatabaseTable_FeatureKeywords keyword)
            return;
        if (e.PropertyName is not (nameof(DatabaseTable_FeatureKeywords.Keyword)
            or nameof(DatabaseTable_FeatureKeywords.Type)))
            return;

        var normalized = CollaborationMarkers.Normalize(keyword.Keyword);
        if (string.IsNullOrWhiteSpace(normalized))
            return;
        if (!string.Equals(keyword.Keyword, normalized, StringComparison.Ordinal))
            keyword.Keyword = normalized;

        if (FeatureKeywords.Any(k =>
                k.Id != keyword.Id &&
                string.Equals(CollaborationMarkers.Normalize(k.Keyword), normalized, StringComparison.OrdinalIgnoreCase))) {
            return;
        }

        DBConnector.UpdateFeatureKeyword(keyword);
        keyword.NotifyTypeLabel();
        RefreshFilteredKeywords();
    }

    private bool ArtistMatchesSearch(DatabaseTable_ArtistNames artist, string search) {
        if (ContainsIgnoreCase(artist.PreferredArtistName, search) ||
            ContainsIgnoreCase(artist.RealName, search))
            return true;

        return AlternativeArtistNameVariants.Any(v =>
            v.RefersToArtistName == artist.ID &&
            ContainsIgnoreCase(v.AlternativeArtistName, search));
    }

    private static bool ContainsIgnoreCase(string? value, string search) =>
        !string.IsNullOrEmpty(value) &&
        value.Contains(search, StringComparison.CurrentCultureIgnoreCase);

    private void RefreshFilteredAlternatives() {
        IEnumerable<DatabaseTable_AlternativeArtistNameVariants> query = AlternativeArtistNameVariants;
        if (SelectedArtistName is not null)
            query = query.Where(v => v.RefersToArtistName == SelectedArtistName.ID);
        FilteredAlternativeArtistNameVariants = new ObservableCollection<DatabaseTable_AlternativeArtistNameVariants>(query);
    }

    private void RefreshAssignments() {
        var assignedGenres = SelectedArtistName is null ? [] : DBConnector.LoadGenreIdsForArtist(SelectedArtistName.ID);
        var assignedMoods  = SelectedArtistName is null ? [] : DBConnector.LoadMoodIdsForArtist(SelectedArtistName.ID);
        var canAssign      = HasSelectedArtist;
        foreach (var genre in Genres) {
            genre.CanAssign = canAssign;
            genre.SetAssignedSilent(assignedGenres.Contains(genre.Id));
        }
        foreach (var mood in Moods) {
            mood.CanAssign = canAssign;
            mood.SetAssignedSilent(assignedMoods.Contains(mood.Id));
        }
    }

    private void RefreshMemberships() {
        if (SelectedArtistName is null) {
            MembersOfSelected = [];
            GroupsOfSelected  = [];
            return;
        }

        var memberIds = DBConnector.LoadMemberIdsForGroup(SelectedArtistName.ID);
        var groupIds  = DBConnector.LoadGroupIdsForMember(SelectedArtistName.ID);
        MembersOfSelected = new ObservableCollection<DatabaseTable_ArtistNames>(
            ArtistNames.Where(a => memberIds.Contains(a.ID))
                .OrderBy(a => a.PreferredArtistName, StringComparer.CurrentCultureIgnoreCase));
        GroupsOfSelected = new ObservableCollection<DatabaseTable_ArtistNames>(
            ArtistNames.Where(a => groupIds.Contains(a.ID))
                .OrderBy(a => a.PreferredArtistName, StringComparer.CurrentCultureIgnoreCase));
    }

    private void RaiseCaptions() {
        OnPropertyChanged(nameof(HasSelectedArtist));
        OnPropertyChanged(nameof(HasSelectedGenre));
        OnPropertyChanged(nameof(HasSelectedMood));
        OnPropertyChanged(nameof(HasBlockedArtists));
        OnPropertyChanged(nameof(HasBlockedGenres));
        OnPropertyChanged(nameof(HasBlockedMoods));
        OnPropertyChanged(nameof(ArtistListCaption));
        OnPropertyChanged(nameof(AlternativeListCaption));
        OnPropertyChanged(nameof(MembersCaption));
        OnPropertyChanged(nameof(GroupsCaption));
        OnPropertyChanged(nameof(MembershipNameSuggestions));
        OnPropertyChanged(nameof(AddArtistAsPreferred));
        OnPropertyChanged(nameof(AddArtistAsAlternative));
        OnPropertyChanged(nameof(AddGenreAsPreferred));
        OnPropertyChanged(nameof(AddGenreAsAlternative));
        OnPropertyChanged(nameof(AddMoodAsPreferred));
        OnPropertyChanged(nameof(AddMoodAsAlternative));
        OnPropertyChanged(nameof(AddArtistActionCaption));
        OnPropertyChanged(nameof(AddGenreActionCaption));
        OnPropertyChanged(nameof(AddMoodActionCaption));
        AddArtistCommand.NotifyCanExecuteChanged();
        AddAlternativeCommand.NotifyCanExecuteChanged();
        AddMemberCommand.NotifyCanExecuteChanged();
        AddGroupCommand.NotifyCanExecuteChanged();
        AddGenreCommand.NotifyCanExecuteChanged();
        AddMoodCommand.NotifyCanExecuteChanged();
        AddGenreVariantCommand.NotifyCanExecuteChanged();
        AddMoodVariantCommand.NotifyCanExecuteChanged();
        AddKeywordCommand.NotifyCanExecuteChanged();
        BlockNewArtistCommand.NotifyCanExecuteChanged();
        BlockNewGenreCommand.NotifyCanExecuteChanged();
        BlockNewMoodCommand.NotifyCanExecuteChanged();
        BlockNewAlternativeCommand.NotifyCanExecuteChanged();
        BlockNewGenreVariantCommand.NotifyCanExecuteChanged();
        BlockNewMoodVariantCommand.NotifyCanExecuteChanged();
        foreach (var keyword in FeatureKeywords)
            keyword.NotifyTypeLabel();
    }

    partial void OnSelectedArtistNameChanged(DatabaseTable_ArtistNames? value) {
        if (value is not null)
            SelectedArtistForNewAlternative = value;
        RefreshFilteredAlternatives();
        RefreshAssignments();
        RefreshMemberships();
        RaiseCaptions();
    }

    partial void OnSelectedGenreChanged(AssignableTag? value) {
        if (value is not null)
            SelectedGenreForNewAlternative = value;
        RefreshFilteredGenreVariants();
        RaiseCaptions();
    }

    partial void OnSelectedMoodChanged(AssignableTag? value) {
        if (value is not null)
            SelectedMoodForNewAlternative = value;
        RefreshFilteredMoodVariants();
        RaiseCaptions();
    }

    partial void OnArtistInsertModeChanged(CatalogInsertMode value) {
        OnPropertyChanged(nameof(AddArtistAsPreferred));
        OnPropertyChanged(nameof(AddArtistAsAlternative));
        OnPropertyChanged(nameof(AddArtistActionCaption));
        AddArtistCommand.NotifyCanExecuteChanged();
    }

    partial void OnGenreInsertModeChanged(CatalogInsertMode value) {
        OnPropertyChanged(nameof(AddGenreAsPreferred));
        OnPropertyChanged(nameof(AddGenreAsAlternative));
        OnPropertyChanged(nameof(AddGenreActionCaption));
        AddGenreCommand.NotifyCanExecuteChanged();
    }

    partial void OnMoodInsertModeChanged(CatalogInsertMode value) {
        OnPropertyChanged(nameof(AddMoodAsPreferred));
        OnPropertyChanged(nameof(AddMoodAsAlternative));
        OnPropertyChanged(nameof(AddMoodActionCaption));
        AddMoodCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedArtistForNewAlternativeChanged(DatabaseTable_ArtistNames? value) =>
        AddArtistCommand.NotifyCanExecuteChanged();

    partial void OnSelectedGenreForNewAlternativeChanged(AssignableTag? value) =>
        AddGenreCommand.NotifyCanExecuteChanged();

    partial void OnSelectedMoodForNewAlternativeChanged(AssignableTag? value) =>
        AddMoodCommand.NotifyCanExecuteChanged();

    private void UpdateFilterFlags() {
        foreach (var genre in Genres)
            genre.IsFilterActive = GenreFilter?.Id == genre.Id;
        foreach (var mood in Moods)
            mood.IsFilterActive = MoodFilter?.Id == mood.Id;
    }

    partial void OnGenreFilterChanged(AssignableTag? value) {
        UpdateFilterFlags();
        RefreshFilteredArtists();
        OnPropertyChanged(nameof(ArtistListCaption));
    }

    partial void OnMoodFilterChanged(AssignableTag? value) {
        UpdateFilterFlags();
        RefreshFilteredArtists();
        OnPropertyChanged(nameof(ArtistListCaption));
    }

    partial void OnArtistSearchTextChanged(string value) {
        OnPropertyChanged(nameof(HasSearchText));
        RefreshFilteredArtists();
        OnPropertyChanged(nameof(ArtistListCaption));
        ClearSearchCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void RefreshCatalog() => LoadAll();

    private bool CanClearSearch() => HasSearchText;

    [RelayCommand(CanExecute = nameof(CanClearSearch))]
    private void ClearSearch() => ArtistSearchText = "";

    partial void OnNewPreferredArtistNameChanged(string value) {
        AddArtistCommand.NotifyCanExecuteChanged();
        BlockNewArtistCommand.NotifyCanExecuteChanged();
    }
    partial void OnNewAlternativeNameChanged(string value) {
        AddAlternativeCommand.NotifyCanExecuteChanged();
        BlockNewAlternativeCommand.NotifyCanExecuteChanged();
    }
    partial void OnNewMemberNameChanged(string value) => AddMemberCommand.NotifyCanExecuteChanged();
    partial void OnNewGroupNameChanged(string value) => AddGroupCommand.NotifyCanExecuteChanged();
    partial void OnNewGenreNameChanged(string value) {
        AddGenreCommand.NotifyCanExecuteChanged();
        BlockNewGenreCommand.NotifyCanExecuteChanged();
    }
    partial void OnNewMoodNameChanged(string value) {
        AddMoodCommand.NotifyCanExecuteChanged();
        BlockNewMoodCommand.NotifyCanExecuteChanged();
    }
    partial void OnNewGenreVariantChanged(string value) {
        AddGenreVariantCommand.NotifyCanExecuteChanged();
        BlockNewGenreVariantCommand.NotifyCanExecuteChanged();
    }
    partial void OnNewMoodVariantChanged(string value) {
        AddMoodVariantCommand.NotifyCanExecuteChanged();
        BlockNewMoodVariantCommand.NotifyCanExecuteChanged();
    }
    partial void OnNewKeywordChanged(string value) => AddKeywordCommand.NotifyCanExecuteChanged();

    private void OnArtistPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is not DatabaseTable_ArtistNames artist) return;
        if (e.PropertyName is nameof(DatabaseTable_ArtistNames.PreferredArtistName) or nameof(DatabaseTable_ArtistNames.RealName))
            DBConnector.UpdateArtistName(artist);
        if (e.PropertyName == nameof(DatabaseTable_ArtistNames.PreferredArtistName))
            OnPropertyChanged(nameof(AlternativeListCaption));
    }

    private void OnAlternativePropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is DatabaseTable_AlternativeArtistNameVariants variant &&
            e.PropertyName == nameof(DatabaseTable_AlternativeArtistNameVariants.AlternativeArtistName))
            DBConnector.UpdateAlternativeArtistName(variant);
    }

    private void OnTagAssignedChanged(AssignableTag tag, bool assigned) {
        if (SelectedArtistName is null) {
            tag.SetAssignedSilent(false);
            return;
        }

        if (tag.Kind == TagKind.Genre)
            DBConnector.SetArtistGenre(SelectedArtistName.ID, tag.Id, assigned);
        else
            DBConnector.SetArtistMood(SelectedArtistName.ID, tag.Id, assigned);

        RefreshFilteredArtists();
    }

    private bool CanAddArtist() =>
        !string.IsNullOrWhiteSpace(NewPreferredArtistName) &&
        (ArtistInsertMode != CatalogInsertMode.Alternative || SelectedArtistForNewAlternative is not null);

    private bool CanAddAlternative() => HasSelectedArtist && !string.IsNullOrWhiteSpace(NewAlternativeName);
    private bool CanAddMember() => HasSelectedArtist && !string.IsNullOrWhiteSpace(NewMemberName);
    private bool CanAddGroup() => HasSelectedArtist && !string.IsNullOrWhiteSpace(NewGroupName);

    private bool CanAddGenre() =>
        !string.IsNullOrWhiteSpace(NewGenreName) &&
        (GenreInsertMode != CatalogInsertMode.Alternative || SelectedGenreForNewAlternative is not null);

    private bool CanAddMood() =>
        !string.IsNullOrWhiteSpace(NewMoodName) &&
        (MoodInsertMode != CatalogInsertMode.Alternative || SelectedMoodForNewAlternative is not null);

    private bool CanBlockNewArtist()      => !string.IsNullOrWhiteSpace(NewPreferredArtistName);
    private bool CanBlockNewGenre()       => !string.IsNullOrWhiteSpace(NewGenreName);
    private bool CanBlockNewMood()        => !string.IsNullOrWhiteSpace(NewMoodName);
    private bool CanBlockNewAlternative() => !string.IsNullOrWhiteSpace(NewAlternativeName);
    private bool CanBlockNewGenreVariant() => !string.IsNullOrWhiteSpace(NewGenreVariant);
    private bool CanBlockNewMoodVariant()  => !string.IsNullOrWhiteSpace(NewMoodVariant);
    private bool CanAddGenreVariant() => HasSelectedGenre && !string.IsNullOrWhiteSpace(NewGenreVariant);
    private bool CanAddMoodVariant() => HasSelectedMood && !string.IsNullOrWhiteSpace(NewMoodVariant);
    private bool CanAddKeyword() => !string.IsNullOrWhiteSpace(NewKeyword);

    [RelayCommand(CanExecute = nameof(CanAddArtist))]
    private async Task AddArtist() {
        var preferred = NewPreferredArtistName.Trim();
        var realName  = NewRealName.Trim();
        var check     = DBConnector.CheckForPreferredName(preferred);

        if (check.ArtistNameStatus == ArtistNameStatus.AlreadyExisting) {
            await BuranMessageBox.Show(L.Format("Db.ArtistAlreadyExists", preferred), L.Get("Common.Warning"));
            return;
        }
        if (check.ArtistNameStatus == ArtistNameStatus.IsAlternativeName) {
            await BuranMessageBox.Show(
                L.Format("Db.ArtistIsAlternative", preferred, check.PreferredArtistName),
                L.Get("Common.Warning"));
            return;
        }

        if (ArtistInsertMode == CatalogInsertMode.Alternative) {
            if (SelectedArtistForNewAlternative is null) {
                await BuranMessageBox.Show(L.Get("Catalog.NeedPreferredForAlt"), L.Get("Common.Warning"));
                return;
            }

            var ownerId = SelectedArtistForNewAlternative.ID;
            DBConnector.InsertAlternativeArtistName(preferred, ownerId);
            NewPreferredArtistName = "";
            NewRealName            = "";
            LoadAll();
            SelectedArtistName = ArtistNames.FirstOrDefault(a => a.ID == ownerId);
            return;
        }

        DBConnector.InsertArtistName(preferred, realName);
        NewPreferredArtistName = "";
        NewRealName            = "";
        LoadAll();
        SelectedArtistName = ArtistNames.FirstOrDefault(a =>
            string.Equals(a.PreferredArtistName, preferred, StringComparison.CurrentCultureIgnoreCase));
    }

    [RelayCommand]
    private async Task DeleteArtist() {
        if (SelectedArtistName is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectArtistToDelete"), L.Get("Common.Warning"));
            return;
        }
        DBConnector.DeleteArtist(SelectedArtistName.ID);
        SelectedArtistName = null;
        LoadAll();
    }

    [RelayCommand]
    private void UnselectArtist() => SelectedArtistName = null;

    [RelayCommand(CanExecute = nameof(CanAddAlternative))]
    private void AddAlternative() {
        if (SelectedArtistName is null) return;
        var name     = NewAlternativeName.Trim();
        var artistId = SelectedArtistName.ID;
        DBConnector.InsertAlternativeArtistName(name, artistId);
        NewAlternativeName = "";
        LoadAll();
        SelectedArtistName = ArtistNames.FirstOrDefault(a => a.ID == artistId);
    }

    [RelayCommand]
    private async Task DeleteAlternative() {
        if (SelectedAlternativeName is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectAlternativeToDelete"), L.Get("Common.Warning"));
            return;
        }
        DBConnector.DeleteAlternativeArtistName(SelectedAlternativeName.ID);
        SelectedAlternativeName = null;
        LoadAll();
    }

    [RelayCommand(CanExecute = nameof(CanAddMember))]
    private async Task AddMember() {
        if (SelectedArtistName is null) return;
        await AddMembershipAsync(SelectedArtistName.ID, NewMemberName, asMemberOfSelected: true);
        NewMemberName = "";
    }

    [RelayCommand]
    private void RemoveMember() {
        if (SelectedArtistName is null || SelectedMember is null) return;
        var groupId = SelectedArtistName.ID;
        DBConnector.SetArtistMembership(groupId, SelectedMember.ID, assigned: false);
        SelectedMember = null;
        RefreshMemberships();
        SelectedArtistName = ArtistNames.FirstOrDefault(a => a.ID == groupId);
    }

    [RelayCommand(CanExecute = nameof(CanAddGroup))]
    private async Task AddGroup() {
        if (SelectedArtistName is null) return;
        await AddMembershipAsync(SelectedArtistName.ID, NewGroupName, asMemberOfSelected: false);
        NewGroupName = "";
    }

    [RelayCommand]
    private void RemoveGroup() {
        if (SelectedArtistName is null || SelectedGroup is null) return;
        var memberId = SelectedArtistName.ID;
        DBConnector.SetArtistMembership(SelectedGroup.ID, memberId, assigned: false);
        SelectedGroup = null;
        RefreshMemberships();
        SelectedArtistName = ArtistNames.FirstOrDefault(a => a.ID == memberId);
    }

    private async Task AddMembershipAsync(int selectedId, string rawName, bool asMemberOfSelected) {
        var name     = rawName.Trim();
        var resolved = DBConnector.CheckForPreferredName(name);
        if (resolved.ArtistNameStatus == ArtistNameStatus.IsNonExistent) {
            await BuranMessageBox.Show(
                L.Format("Db.NameNotAnArtist", name),
                L.Get("Common.Warning"));
            return;
        }

        var other = ArtistNames.FirstOrDefault(a =>
            string.Equals(a.PreferredArtistName, resolved.PreferredArtistName, StringComparison.OrdinalIgnoreCase));
        if (other is null) {
            await BuranMessageBox.Show(L.Format("Db.NameNotInList", resolved.PreferredArtistName), L.Get("Common.Warning"));
            return;
        }
        if (other.ID == selectedId) {
            await BuranMessageBox.Show(L.Get("Db.GroupCannotBeSelf"), L.Get("Common.Warning"));
            return;
        }

        var groupId  = asMemberOfSelected ? selectedId : other.ID;
        var memberId = asMemberOfSelected ? other.ID : selectedId;
        DBConnector.SetArtistMembership(groupId, memberId, assigned: true);
        RefreshMemberships();
        SelectedArtistName = ArtistNames.FirstOrDefault(a => a.ID == selectedId);
    }

    [RelayCommand(CanExecute = nameof(CanAddGenre))]
    private async Task AddGenre() {
        var name = NewGenreName.Trim();
        var resolved = DBConnector.ResolveGenreName(name);
        if (resolved.Status == ArtistNameStatus.AlreadyExisting) {
            await BuranMessageBox.Show(L.Format("Db.GenreExists", resolved.PreferredName), L.Get("Common.Warning"));
            return;
        }
        if (resolved.Status == ArtistNameStatus.IsAlternativeName) {
            await BuranMessageBox.Show(L.Format("Db.GenreIsAlternative", name, resolved.PreferredName), L.Get("Common.Warning"));
            return;
        }

        if (GenreInsertMode == CatalogInsertMode.Alternative) {
            if (SelectedGenreForNewAlternative is null) {
                await BuranMessageBox.Show(L.Get("Catalog.NeedPreferredForAlt"), L.Get("Common.Warning"));
                return;
            }

            var genreId = SelectedGenreForNewAlternative.Id;
            DBConnector.InsertAlternativeGenreName(name, genreId);
            NewGenreName = "";
            LoadAll();
            SelectedGenre = Genres.FirstOrDefault(g => g.Id == genreId);
            return;
        }

        DBConnector.InsertGenreName(name);
        NewGenreName = "";
        LoadAll();
    }

    [RelayCommand]
    private async Task DeleteGenre() {
        if (SelectedGenre is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectGenreToDelete"), L.Get("Common.Warning"));
            return;
        }
        if (GenreFilter?.Id == SelectedGenre.Id)
            GenreFilter = null;
        DBConnector.DeleteGenre(SelectedGenre.Id);
        SelectedGenre = null;
        LoadAll();
    }

    [RelayCommand]
    private void UnselectGenre() => SelectedGenre = null;

    [RelayCommand]
    private void FilterByGenre() {
        if (SelectedGenre is null) return;
        GenreFilter = SelectedGenre;
    }

    [RelayCommand]
    private void ClearGenreFilter() => GenreFilter = null;

    [RelayCommand(CanExecute = nameof(CanAddMood))]
    private async Task AddMood() {
        var name = NewMoodName.Trim();
        var resolved = DBConnector.ResolveMoodName(name);
        if (resolved.Status == ArtistNameStatus.AlreadyExisting) {
            await BuranMessageBox.Show(L.Format("Db.MoodExists", resolved.PreferredName), L.Get("Common.Warning"));
            return;
        }
        if (resolved.Status == ArtistNameStatus.IsAlternativeName) {
            await BuranMessageBox.Show(L.Format("Db.MoodIsAlternative", name, resolved.PreferredName), L.Get("Common.Warning"));
            return;
        }

        if (MoodInsertMode == CatalogInsertMode.Alternative) {
            if (SelectedMoodForNewAlternative is null) {
                await BuranMessageBox.Show(L.Get("Catalog.NeedPreferredForAlt"), L.Get("Common.Warning"));
                return;
            }

            var moodId = SelectedMoodForNewAlternative.Id;
            DBConnector.InsertAlternativeMoodName(name, moodId);
            NewMoodName = "";
            LoadAll();
            SelectedMood = Moods.FirstOrDefault(m => m.Id == moodId);
            return;
        }

        DBConnector.InsertMoodName(name);
        NewMoodName = "";
        LoadAll();
    }

    [RelayCommand]
    private async Task DeleteMood() {
        if (SelectedMood is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectMoodToDelete"), L.Get("Common.Warning"));
            return;
        }
        if (MoodFilter?.Id == SelectedMood.Id)
            MoodFilter = null;
        DBConnector.DeleteMood(SelectedMood.Id);
        SelectedMood = null;
        LoadAll();
    }

    [RelayCommand]
    private void UnselectMood() => SelectedMood = null;

    [RelayCommand]
    private void FilterByMood() {
        if (SelectedMood is null) return;
        MoodFilter = SelectedMood;
    }

    [RelayCommand]
    private void ClearMoodFilter() => MoodFilter = null;

    [RelayCommand(CanExecute = nameof(CanAddGenreVariant))]
    private async Task AddGenreVariant() {
        if (SelectedGenre is null) return;
        var name = NewGenreVariant.Trim();
        var resolved = DBConnector.ResolveGenreName(name);
        if (resolved.IsKnown) {
            await BuranMessageBox.Show(L.Format("Db.GenreExists", resolved.PreferredName), L.Get("Common.Warning"));
            return;
        }

        var genreId = SelectedGenre.Id;
        DBConnector.InsertAlternativeGenreName(name, genreId);
        NewGenreVariant = "";
        LoadAll();
        SelectedGenre = Genres.FirstOrDefault(g => g.Id == genreId);
    }

    [RelayCommand]
    private async Task DeleteGenreVariant() {
        if (SelectedGenreVariant is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectGenreVariantToDelete"), L.Get("Common.Warning"));
            return;
        }
        var genreId = SelectedGenre?.Id;
        DBConnector.DeleteAlternativeGenreName(SelectedGenreVariant.Id);
        SelectedGenreVariant = null;
        LoadAll();
        SelectedGenre = Genres.FirstOrDefault(g => g.Id == genreId);
    }

    [RelayCommand(CanExecute = nameof(CanAddMoodVariant))]
    private async Task AddMoodVariant() {
        if (SelectedMood is null) return;
        var name = NewMoodVariant.Trim();
        var resolved = DBConnector.ResolveMoodName(name);
        if (resolved.IsKnown) {
            await BuranMessageBox.Show(L.Format("Db.MoodExists", resolved.PreferredName), L.Get("Common.Warning"));
            return;
        }

        var moodId = SelectedMood.Id;
        DBConnector.InsertAlternativeMoodName(name, moodId);
        NewMoodVariant = "";
        LoadAll();
        SelectedMood = Moods.FirstOrDefault(m => m.Id == moodId);
    }

    [RelayCommand]
    private async Task DeleteMoodVariant() {
        if (SelectedMoodVariant is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectMoodVariantToDelete"), L.Get("Common.Warning"));
            return;
        }
        var moodId = SelectedMood?.Id;
        DBConnector.DeleteAlternativeMoodName(SelectedMoodVariant.Id);
        SelectedMoodVariant = null;
        LoadAll();
        SelectedMood = Moods.FirstOrDefault(m => m.Id == moodId);
    }

    [RelayCommand(CanExecute = nameof(CanAddKeyword))]
    private async Task AddKeyword() {
        var keyword = CollaborationMarkers.Normalize(NewKeyword);
        var type    = SelectedKeywordType?.Code ?? CollaborationMarkers.TypeCollaboration;
        if (string.IsNullOrWhiteSpace(keyword))
            return;

        if (FeatureKeywords.Any(k =>
                string.Equals(CollaborationMarkers.Normalize(k.Keyword), keyword, StringComparison.OrdinalIgnoreCase))) {
            await BuranMessageBox.Show(L.Format("Db.KeywordExists", keyword), L.Get("Common.Warning"));
            return;
        }

        DBConnector.InsertFeatureKeyword(keyword, type);
        NewKeyword = "";
        LoadAll();
    }

    [RelayCommand]
    private async Task DeleteKeyword() {
        if (SelectedFeatureKeyword is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectKeywordToDelete"), L.Get("Common.Warning"));
            return;
        }

        DBConnector.DeleteFeatureKeyword(SelectedFeatureKeyword.Id);
        SelectedFeatureKeyword = null;
        LoadAll();
    }

    [RelayCommand]
    private void UnselectKeyword() => SelectedFeatureKeyword = null;

    [RelayCommand(CanExecute = nameof(CanBlockNewArtist))]
    private void BlockNewArtist() {
        var name = NewPreferredArtistName.Trim();
        NewPreferredArtistName = "";
        NewRealName            = "";
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindArtist, name);
        LoadAll();
    }

    [RelayCommand(CanExecute = nameof(CanBlockNewGenre))]
    private void BlockNewGenre() {
        var name = NewGenreName.Trim();
        NewGenreName = "";
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindGenre, name);
        LoadAll();
    }

    [RelayCommand(CanExecute = nameof(CanBlockNewMood))]
    private void BlockNewMood() {
        var name = NewMoodName.Trim();
        NewMoodName = "";
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindMood, name);
        LoadAll();
    }

    [RelayCommand(CanExecute = nameof(CanBlockNewAlternative))]
    private void BlockNewAlternative() {
        var name = NewAlternativeName.Trim();
        NewAlternativeName = "";
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindArtist, name);
        LoadAll();
    }

    [RelayCommand(CanExecute = nameof(CanBlockNewGenreVariant))]
    private void BlockNewGenreVariant() {
        var name    = NewGenreVariant.Trim();
        var genreId = SelectedGenre?.Id;
        NewGenreVariant = "";
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindGenre, name);
        LoadAll();
        SelectedGenre = Genres.FirstOrDefault(g => g.Id == genreId);
    }

    [RelayCommand(CanExecute = nameof(CanBlockNewMoodVariant))]
    private void BlockNewMoodVariant() {
        var name   = NewMoodVariant.Trim();
        var moodId = SelectedMood?.Id;
        NewMoodVariant = "";
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindMood, name);
        LoadAll();
        SelectedMood = Moods.FirstOrDefault(m => m.Id == moodId);
    }

    [RelayCommand]
    private async Task BlockSelectedArtist() {
        if (SelectedArtistName is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectToBlock"), L.Get("Common.Warning"));
            return;
        }

        var name = SelectedArtistName.PreferredArtistName;
        SelectedArtistName = null;
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindArtist, name);
        LoadAll();
    }

    [RelayCommand]
    private async Task BlockSelectedAlternative() {
        if (SelectedAlternativeName is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectToBlock"), L.Get("Common.Warning"));
            return;
        }

        var name = SelectedAlternativeName.AlternativeArtistName;
        SelectedAlternativeName = null;
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindArtist, name);
        LoadAll();
    }

    [RelayCommand]
    private async Task BlockSelectedGenre() {
        if (SelectedGenre is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectToBlock"), L.Get("Common.Warning"));
            return;
        }

        var name = SelectedGenre.Name;
        if (GenreFilter?.Id == SelectedGenre.Id)
            GenreFilter = null;
        SelectedGenre = null;
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindGenre, name);
        LoadAll();
    }

    [RelayCommand]
    private async Task BlockSelectedGenreVariant() {
        if (SelectedGenreVariant is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectToBlock"), L.Get("Common.Warning"));
            return;
        }

        var name    = SelectedGenreVariant.GenreNameVariant;
        var genreId = SelectedGenre?.Id;
        SelectedGenreVariant = null;
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindGenre, name);
        LoadAll();
        SelectedGenre = Genres.FirstOrDefault(g => g.Id == genreId);
    }

    [RelayCommand]
    private async Task BlockSelectedMood() {
        if (SelectedMood is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectToBlock"), L.Get("Common.Warning"));
            return;
        }

        var name = SelectedMood.Name;
        if (MoodFilter?.Id == SelectedMood.Id)
            MoodFilter = null;
        SelectedMood = null;
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindMood, name);
        LoadAll();
    }

    [RelayCommand]
    private async Task BlockSelectedMoodVariant() {
        if (SelectedMoodVariant is null) {
            await BuranMessageBox.Show(L.Get("Db.SelectToBlock"), L.Get("Common.Warning"));
            return;
        }

        var name   = SelectedMoodVariant.MoodNameVariant;
        var moodId = SelectedMood?.Id;
        SelectedMoodVariant = null;
        DBConnector.BlockCatalogValue(DatabaseTable_BlockedCatalogValues.KindMood, name);
        LoadAll();
        SelectedMood = Moods.FirstOrDefault(m => m.Id == moodId);
    }

    [RelayCommand]
    private void UnblockValue(DatabaseTable_BlockedCatalogValues? item) {
        if (item is null)
            return;

        DBConnector.DeleteBlockedCatalogValue(item.Id);
        LoadAll();
    }

    [RelayCommand]
    private void ShowTracksForArtist() {
        if (SelectedArtistName is not null)
            ShowIndexedTracks("artist", SelectedArtistName.PreferredArtistName);
    }

    [RelayCommand]
    private void ShowTracksForGenre() {
        if (SelectedGenre is not null)
            ShowIndexedTracks("genre", SelectedGenre.Name);
    }

    [RelayCommand]
    private void ShowTracksForMood() {
        if (SelectedMood is not null)
            ShowIndexedTracks("mood", SelectedMood.Name);
    }

    private void ShowIndexedTracks(string kind, string name) {
        var lookup = DBConnector.FindIndexedTracks(kind, name);
        var paths  = lookup.Hits.Select(h => h.Path).Where(File.Exists).ToList();
        var host   = ModuleHub.Find<ITrackListHost>();
        if (host is null)
            return;

        string? notice = null;
        if (lookup.RemovedGhosts.Count > 0) {
            notice = string.Join(
                "\n",
                lookup.RemovedGhosts.Select(ghost =>
                    L.Format("Db.IndexCaseGhost", ghost.RemovedFileName, ghost.SurvivingFileName)));
        }

        host.ShowTracks(paths, L.Format("Db.TracksFor", name), notice);
        ModuleHub.ShowId3Editor();
    }
}
