using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Buran.DBEditor.Models;
using Buran.SQLite;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.DBEditor.ViewModels;

[ObservableObject]
public partial class DBEditorTabViewModel {
    [ObservableProperty] private DatabaseTable_ArtistNames? _selectedArtistName;
    [ObservableProperty] private DatabaseTable_AlternativeArtistNameVariants? _selectedAlternativeName;
    [ObservableProperty] private AssignableTag? _selectedGenre;
    [ObservableProperty] private AssignableTag? _selectedMood;

    [ObservableProperty] private ObservableCollection<DatabaseTable_ArtistNames> _artistNames = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_ArtistNames> _filteredArtistNames = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_AlternativeArtistNameVariants> _alternativeArtistNameVariants = [];
    [ObservableProperty] private ObservableCollection<DatabaseTable_AlternativeArtistNameVariants> _filteredAlternativeArtistNameVariants = [];
    [ObservableProperty] private ObservableCollection<AssignableTag> _genres = [];
    [ObservableProperty] private ObservableCollection<AssignableTag> _moods = [];

    [ObservableProperty] private string _newPreferredArtistName = "";
    [ObservableProperty] private string _newRealName = "";
    [ObservableProperty] private string _newAlternativeName = "";
    [ObservableProperty] private string _newGenreName = "";
    [ObservableProperty] private string _newMoodName = "";

    public bool HasSelectedArtist => SelectedArtistName is not null;

    public string ArtistListCaption {
        get {
            var filters = new List<string>();
            if (SelectedGenre is not null) filters.Add("Genre: " + SelectedGenre.Name);
            if (SelectedMood is not null) filters.Add("Mood: " + SelectedMood.Name);
            return filters.Count == 0 ? "Künstler" : "Künstler (" + string.Join(", ", filters) + ")";
        }
    }

    public string AlternativeListCaption =>
        SelectedArtistName is null
            ? "Alternative Namen (alle)"
            : "Alternative Namen von " + SelectedArtistName.PreferredArtistName;

    public DBEditorTabViewModel() {
        try {
            LoadAll();
        }
        catch (Exception ex) {
            Console.WriteLine("DBEditor: Laden fehlgeschlagen: " + ex.Message);
        }
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
        SelectedArtistName      = ArtistNames.FirstOrDefault(a => a.ID == artistId);
        SelectedAlternativeName = AlternativeArtistNameVariants.FirstOrDefault(a => a.ID == altId);
        RefreshFilteredArtists();
        RefreshFilteredAlternatives();
        RefreshAssignments();
        RaiseCaptions();
    }

    private void UnsubscribeCatalog() {
        foreach (var artist in ArtistNames)
            artist.PropertyChanged -= OnArtistPropertyChanged;
        foreach (var variant in AlternativeArtistNameVariants)
            variant.PropertyChanged -= OnAlternativePropertyChanged;
    }

    private void RebuildTags() {
        var selectedGenreId = SelectedGenre?.Id;
        var selectedMoodId  = SelectedMood?.Id;
        var assignedGenres  = SelectedArtistName is null ? [] : DBConnector.LoadGenreIdsForArtist(SelectedArtistName.ID);
        var assignedMoods   = SelectedArtistName is null ? [] : DBConnector.LoadMoodIdsForArtist(SelectedArtistName.ID);

        Genres = new ObservableCollection<AssignableTag>(
            DBConnector.LoadTableContent_GenreNames()
                .Select(g => CreateTag(TagKind.Genre, g.ID, g.GenreName, assignedGenres.Contains(g.ID))));
        Moods = new ObservableCollection<AssignableTag>(
            DBConnector.LoadTableContent_MoodNames()
                .Select(m => CreateTag(TagKind.Mood, m.ID, m.MoodName, assignedMoods.Contains(m.ID))));

        SelectedGenre = Genres.FirstOrDefault(g => g.Id == selectedGenreId);
        SelectedMood  = Moods.FirstOrDefault(m => m.Id == selectedMoodId);
    }

    private AssignableTag CreateTag(TagKind kind, int id, string name, bool assigned) {
        var tag = new AssignableTag(kind, id, name, assigned, OnTagAssignedChanged);
        tag.PropertyChanged += OnTagPropertyChanged;
        return tag;
    }

    private void OnTagPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is not AssignableTag tag || e.PropertyName != nameof(AssignableTag.Name)) return;
        if (tag.Kind == TagKind.Genre)
            DBConnector.UpdateGenreName(new DatabaseTable_GenreNames { ID = tag.Id, GenreName = tag.Name });
        else
            DBConnector.UpdateMoodName(new DatabaseTable_MoodNames { ID = tag.Id, MoodName = tag.Name });
        OnPropertyChanged(nameof(ArtistListCaption));
    }

    private void RefreshFilteredArtists() {
        IEnumerable<DatabaseTable_ArtistNames> query = ArtistNames;
        if (SelectedGenre is not null) {
            var ids = DBConnector.LoadArtistIdsForGenre(SelectedGenre.Id);
            query = query.Where(a => ids.Contains(a.ID));
        }
        if (SelectedMood is not null) {
            var ids = DBConnector.LoadArtistIdsForMood(SelectedMood.Id);
            query = query.Where(a => ids.Contains(a.ID));
        }
        FilteredArtistNames = new ObservableCollection<DatabaseTable_ArtistNames>(query);
    }

    private void RefreshFilteredAlternatives() {
        IEnumerable<DatabaseTable_AlternativeArtistNameVariants> query = AlternativeArtistNameVariants;
        if (SelectedArtistName is not null)
            query = query.Where(v => v.RefersToArtistName == SelectedArtistName.ID);
        FilteredAlternativeArtistNameVariants = new ObservableCollection<DatabaseTable_AlternativeArtistNameVariants>(query);
    }

    private void RefreshAssignments() {
        var assignedGenres = SelectedArtistName is null ? [] : DBConnector.LoadGenreIdsForArtist(SelectedArtistName.ID);
        var assignedMoods  = SelectedArtistName is null ? [] : DBConnector.LoadMoodIdsForArtist(SelectedArtistName.ID);
        foreach (var genre in Genres)
            genre.SetAssignedSilent(assignedGenres.Contains(genre.Id));
        foreach (var mood in Moods)
            mood.SetAssignedSilent(assignedMoods.Contains(mood.Id));
    }

    private void RaiseCaptions() {
        OnPropertyChanged(nameof(HasSelectedArtist));
        OnPropertyChanged(nameof(ArtistListCaption));
        OnPropertyChanged(nameof(AlternativeListCaption));
        AddArtistCommand.NotifyCanExecuteChanged();
        AddAlternativeCommand.NotifyCanExecuteChanged();
        AddGenreCommand.NotifyCanExecuteChanged();
        AddMoodCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedArtistNameChanged(DatabaseTable_ArtistNames? value) {
        RefreshFilteredAlternatives();
        RefreshAssignments();
        RaiseCaptions();
    }

    partial void OnSelectedGenreChanged(AssignableTag? value) {
        RefreshFilteredArtists();
        OnPropertyChanged(nameof(ArtistListCaption));
    }

    partial void OnSelectedMoodChanged(AssignableTag? value) {
        RefreshFilteredArtists();
        OnPropertyChanged(nameof(ArtistListCaption));
    }

    partial void OnNewPreferredArtistNameChanged(string value) => AddArtistCommand.NotifyCanExecuteChanged();
    partial void OnNewAlternativeNameChanged(string value) => AddAlternativeCommand.NotifyCanExecuteChanged();
    partial void OnNewGenreNameChanged(string value) => AddGenreCommand.NotifyCanExecuteChanged();
    partial void OnNewMoodNameChanged(string value) => AddMoodCommand.NotifyCanExecuteChanged();

    private void OnArtistPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is not DatabaseTable_ArtistNames artist) return;
        if (e.PropertyName is nameof(DatabaseTable_ArtistNames.PreferredArtistName) or nameof(DatabaseTable_ArtistNames.RealName))
            DBConnector.UpdateArtistName(artist);
        if (e.PropertyName == nameof(DatabaseTable_ArtistNames.PreferredArtistName))
            OnPropertyChanged(nameof(AlternativeListCaption));
    }

    private void OnAlternativePropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (sender is DatabaseTable_AlternativeArtistNameVariants variant)
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
    private bool CanAddGenre() => !string.IsNullOrWhiteSpace(NewGenreName);
    private bool CanAddMood() => !string.IsNullOrWhiteSpace(NewMoodName);

    [RelayCommand(CanExecute = nameof(CanAddArtist))]
    private void AddArtist() {
        DBConnector.InsertInto_ArtistNames(NewPreferredArtistName.Trim(), NewRealName.Trim());
        NewPreferredArtistName = "";
        NewRealName            = "";
        LoadAll();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteArtist() {
        if (SelectedArtistName is null) {
            await BuranMessageBox.Show("Wählen Sie erst einen Künstler aus, den Sie löschen möchten.", "Achtung!");
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
        DBConnector.InsertAlternativeArtistName(NewAlternativeName.Trim(), SelectedArtistName.ID);
        NewAlternativeName = "";
        LoadAll();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteAlternative() {
        if (SelectedAlternativeName is null) {
            await BuranMessageBox.Show("Wählen Sie erst einen alternativen Namen aus, den Sie löschen möchten.", "Achtung!");
            return;
        }
        DBConnector.DeleteAlternativeArtistName(SelectedAlternativeName.ID);
        SelectedAlternativeName = null;
        LoadAll();
    }

    [RelayCommand(CanExecute = nameof(CanAddGenre))]
    private void AddGenre() {
        DBConnector.InsertGenreName(NewGenreName.Trim());
        NewGenreName = "";
        LoadAll();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteGenre() {
        if (SelectedGenre is null) {
            await BuranMessageBox.Show("Wählen Sie erst ein Genre aus, das Sie löschen möchten.", "Achtung!");
            return;
        }
        DBConnector.DeleteGenre(SelectedGenre.Id);
        SelectedGenre = null;
        LoadAll();
    }

    [RelayCommand]
    private void UnselectGenre() => SelectedGenre = null;

    [RelayCommand(CanExecute = nameof(CanAddMood))]
    private void AddMood() {
        DBConnector.InsertMoodName(NewMoodName.Trim());
        NewMoodName = "";
        LoadAll();
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task DeleteMood() {
        if (SelectedMood is null) {
            await BuranMessageBox.Show("Wählen Sie erst ein Mood aus, das Sie löschen möchten.", "Achtung!");
            return;
        }
        DBConnector.DeleteMood(SelectedMood.Id);
        SelectedMood = null;
        LoadAll();
    }

    [RelayCommand]
    private void UnselectMood() => SelectedMood = null;
}
