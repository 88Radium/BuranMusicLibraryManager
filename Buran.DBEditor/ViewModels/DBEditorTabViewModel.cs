using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Buran.DBEditor.Models;
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
    [ObservableProperty] private ObservableCollection<DatabaseTable_FeatureKeywords> _featureKeywords = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_FeatureKeywords> _filteredFeatureKeywords = [];
    [ObservableProperty] private DatabaseTable_FeatureKeywords? _selectedFeatureKeyword;

    [ObservableProperty] private string _newPreferredArtistName = "";
    [ObservableProperty] private string _newRealName = "";
    [ObservableProperty] private string _newAlternativeName = "";
    [ObservableProperty] private string _newMemberName = "";
    [ObservableProperty] private string _newGroupName = "";
    [ObservableProperty] private string _newGenreName = "";
    [ObservableProperty] private string _newMoodName = "";
    [ObservableProperty] private string _newKeyword = "";
    [ObservableProperty] private string _artistSearchText = "";

    public static readonly KeywordTypeOption[] KeywordTypes = [
        new(CollaborationMarkers.TypeCollaboration),
        new(CollaborationMarkers.TypeVersion)
    ];

    [ObservableProperty] private KeywordTypeOption _selectedKeywordType = KeywordTypes[0];

    private int _catalogReloadQueued;

    public bool HasSelectedArtist => SelectedArtistName is not null;

    public bool HasSearchText => !string.IsNullOrWhiteSpace(ArtistSearchText);

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
        var artistId = SelectedArtistName?.ID;
        var altId    = SelectedAlternativeName?.ID;

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
        SelectedArtistName      = ArtistNames.FirstOrDefault(a => a.ID == artistId);
        SelectedAlternativeName = AlternativeArtistNameVariants.FirstOrDefault(a => a.ID == altId);
        RefreshFilteredArtists();
        RefreshFilteredAlternatives();
        RefreshFilteredKeywords();
        RefreshAssignments();
        RefreshMemberships();
        RaiseCaptions();
    }

    private void UnsubscribeCatalog() {
        foreach (var artist in ArtistNames)
            artist.PropertyChanged -= OnArtistPropertyChanged;
        foreach (var variant in AlternativeArtistNameVariants)
            variant.PropertyChanged -= OnAlternativePropertyChanged;
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
        OnPropertyChanged(nameof(ArtistListCaption));
        OnPropertyChanged(nameof(AlternativeListCaption));
        OnPropertyChanged(nameof(MembersCaption));
        OnPropertyChanged(nameof(GroupsCaption));
        OnPropertyChanged(nameof(MembershipNameSuggestions));
        AddArtistCommand.NotifyCanExecuteChanged();
        AddAlternativeCommand.NotifyCanExecuteChanged();
        AddMemberCommand.NotifyCanExecuteChanged();
        AddGroupCommand.NotifyCanExecuteChanged();
        AddGenreCommand.NotifyCanExecuteChanged();
        AddMoodCommand.NotifyCanExecuteChanged();
        AddKeywordCommand.NotifyCanExecuteChanged();
        foreach (var keyword in FeatureKeywords)
            keyword.NotifyTypeLabel();
    }

    partial void OnSelectedArtistNameChanged(DatabaseTable_ArtistNames? value) {
        RefreshFilteredAlternatives();
        RefreshAssignments();
        RefreshMemberships();
        RaiseCaptions();
    }

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

    partial void OnNewPreferredArtistNameChanged(string value) => AddArtistCommand.NotifyCanExecuteChanged();
    partial void OnNewAlternativeNameChanged(string value) => AddAlternativeCommand.NotifyCanExecuteChanged();
    partial void OnNewMemberNameChanged(string value) => AddMemberCommand.NotifyCanExecuteChanged();
    partial void OnNewGroupNameChanged(string value) => AddGroupCommand.NotifyCanExecuteChanged();
    partial void OnNewGenreNameChanged(string value) => AddGenreCommand.NotifyCanExecuteChanged();
    partial void OnNewMoodNameChanged(string value) => AddMoodCommand.NotifyCanExecuteChanged();
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

    private bool CanAddArtist() => !string.IsNullOrWhiteSpace(NewPreferredArtistName);
    private bool CanAddAlternative() => HasSelectedArtist && !string.IsNullOrWhiteSpace(NewAlternativeName);
    private bool CanAddMember() => HasSelectedArtist && !string.IsNullOrWhiteSpace(NewMemberName);
    private bool CanAddGroup() => HasSelectedArtist && !string.IsNullOrWhiteSpace(NewGroupName);
    private bool CanAddGenre() => !string.IsNullOrWhiteSpace(NewGenreName);
    private bool CanAddMood() => !string.IsNullOrWhiteSpace(NewMoodName);
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
        if (Genres.Any(g => string.Equals(g.Name, name, StringComparison.CurrentCultureIgnoreCase))) {
            await BuranMessageBox.Show(L.Format("Db.GenreExists", name), L.Get("Common.Warning"));
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
        if (Moods.Any(m => string.Equals(m.Name, name, StringComparison.CurrentCultureIgnoreCase))) {
            await BuranMessageBox.Show(L.Format("Db.MoodExists", name), L.Get("Common.Warning"));
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
}
