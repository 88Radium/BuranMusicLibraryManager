using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Buran.ID3Editor.Types;
using Buran.SQLite;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.ID3Editor.ViewModels;

public class AddArtistToID3DialogWindowViewModel : ObservableObject {
    private readonly List<Mp3FileObject> _selectedFiles;
    private          List<string>        _allArtistsFromDB;
    private          string              _selectedAutoCompleteItem;

    public AddArtistToID3DialogWindowViewModel(IEnumerable<Mp3FileObject> selectedFiles) {
        _selectedFiles    = selectedFiles.ToList();
        SelectedFiles     = new ObservableCollection<Mp3FileObject>(_selectedFiles);
        CurrentArtists    = new ObservableCollection<string>();
        SuggestedArtists  = new ObservableCollection<string>();
        AutoCompleteItems = new ObservableCollection<string>();
        PreviewItems      = new ObservableCollection<ArtistPreviewItem>();

        // Commands
        LoadArtistsCommand     = new RelayCommand(LoadAllArtistsFromDB);
        AddArtistCommand       = new RelayCommand(AddArtist, () => CanAddArtist); // ) => CanAddArtist()
        RemoveArtistCommand    = new RelayCommand<string>(RemoveArtist);
        ClearAllArtistsCommand = new RelayCommand(ClearAllArtists);
        LoadFromFileCommand    = new RelayCommand(LoadArtistsFromFirstFile);
        ApplyCommand           = new RelayCommand(ApplyArtists);
        CancelCommand          = new RelayCommand(Cancel);

        // Initial load
        LoadAllArtistsFromDB();
        LoadArtistsFromFirstFile();

        // Auto-update preview
        CurrentArtists.CollectionChanged += (s, e) => UpdatePreview();
        PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(NewArtistInput)) {
                UpdatePreview();
                UpdateAutoCompleteSuggestions(NewArtistInput);
            }
        };
    }

    #region Properties

    public ObservableCollection<Mp3FileObject>     SelectedFiles     { get; }
    public ObservableCollection<string>            CurrentArtists    { get; set; }
    public ObservableCollection<string>            SuggestedArtists  { get; }
    public ObservableCollection<string>            AutoCompleteItems { get; }
    public ObservableCollection<ArtistPreviewItem> PreviewItems      { get; }

    public string SelectedAutoCompleteItem {
        get => _selectedAutoCompleteItem;
        set {
            _selectedAutoCompleteItem = value;
            OnPropertyChanged();
        }
    }

    public bool HasAutoCompleteItems => AutoCompleteItems.Any();

    private string _newArtistInput;

    public string NewArtistInput {
        get => _newArtistInput;
        set {
            _newArtistInput = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanAddArtist));
            OnPropertyChanged(nameof(IsNewArtist));
            (AddArtistCommand as IRelayCommand)?.NotifyCanExecuteChanged();
        }
    }

    public bool IsNewArtist => !string.IsNullOrWhiteSpace(NewArtistInput) && _allArtistsFromDB != null && !_allArtistsFromDB.Any(a => a.Equals(NewArtistInput, StringComparison.OrdinalIgnoreCase));

    public bool CanAddArtist => !string.IsNullOrWhiteSpace(NewArtistInput) && !CurrentArtists.Contains(NewArtistInput, StringComparer.OrdinalIgnoreCase);

    public bool CanApply => CurrentArtists.Any() && SelectedFiles.Any();

    #endregion

    #region Commands & Methods

    public ICommand LoadArtistsCommand     { get; set; }
    public ICommand AddArtistCommand       { get; }
    public ICommand RemoveArtistCommand    { get; }
    public ICommand ClearAllArtistsCommand { get; }
    public ICommand LoadFromFileCommand    { get; }
    public ICommand ApplyCommand           { get; }
    public ICommand CancelCommand          { get; }

    private void LoadAllArtistsFromDB() {
        try {
            _allArtistsFromDB = new List<string>();
            SuggestedArtists.Clear();

            var artists = DBConnector.LoadTableContent_ArtistNames();

            foreach (var artist in artists.OrderBy(a => a.PreferredArtistName)) {
                string name = artist.PreferredArtistName;
                _allArtistsFromDB.Add(name);
                SuggestedArtists.Add(name);
            }
        } catch (Exception ex) {
            Console.WriteLine($"Fehler beim Laden der Künstler: {ex.Message}");
        }
    }

    public void UpdateAutoCompleteSuggestions(string input) {
        if (string.IsNullOrWhiteSpace(input) || _allArtistsFromDB == null) {
            AutoCompleteItems.Clear();
            OnPropertyChanged(nameof(HasAutoCompleteItems));
            SelectedAutoCompleteItem = null; // Reset selection
            return;
        }

        var suggestions = _allArtistsFromDB
            .Where(a => a.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(a => a.StartsWith(input, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(a => a)
            .Take(15)
            .ToList();

        AutoCompleteItems.Clear();
        foreach (var suggestion in suggestions) {
            AutoCompleteItems.Add(suggestion);
        }

        OnPropertyChanged(nameof(HasAutoCompleteItems));

        // NICHT SelectedAutoCompleteItem automatisch setzen!
        // Das macht der User per Klick oder Pfeiltasten
    }

    private void AddArtist() {
        if (!CanAddArtist) return;

        // NEUEN Künstler in DB eintragen (falls nicht vorhanden)
        if (IsNewArtist) {
            var artistEvent = DBConnector.CheckForPreferredName(NewArtistInput);
            if (artistEvent.ArtistNameStatus == ArtistNameStatus.IsNonExistent) {
                DBConnector.InsertInto_ArtistNames(NewArtistInput, "");

                // Lokale Liste aktualisieren
                _allArtistsFromDB.Add(NewArtistInput);
                _allArtistsFromDB = _allArtistsFromDB.OrderBy(a => a).ToList();
                SuggestedArtists.Add(NewArtistInput);

                Console.WriteLine($"Neuer Künstler '{NewArtistInput}' in Datenbank gespeichert.");
            }
        }

        CurrentArtists.Add(NewArtistInput);
        NewArtistInput = string.Empty;
        AutoCompleteItems.Clear();
        OnPropertyChanged(nameof(HasAutoCompleteItems));
    }

    private void RemoveArtist(string artist) {
        if (artist != null)
            CurrentArtists.Remove(artist);
    }

    private void ClearAllArtists() {
        CurrentArtists.Clear();
    }

    private void LoadArtistsFromFirstFile() {
        if (!_selectedFiles.Any()) return;

        var firstFile = _selectedFiles.First();
        if (firstFile.Id3ArtistList != null) {
            CurrentArtists.Clear();
            foreach (var artist in firstFile.Id3ArtistList) {
                if (!string.IsNullOrWhiteSpace(artist) &&
                    !CurrentArtists.Contains(artist, StringComparer.OrdinalIgnoreCase))
                    CurrentArtists.Add(artist);
            }
        }
    }

    private void ApplyArtists() {
        if (!CanApply) return;

        try {
            // 1. Alle Künstler in DB prüfen/speichern
            foreach (var artist in CurrentArtists) {
                var artistEvent = DBConnector.CheckForPreferredName(artist);

                if (artistEvent.ArtistNameStatus == ArtistNameStatus.IsNonExistent) {
                    DBConnector.InsertInto_ArtistNames(artistEvent.PreferredArtistName, "");
                }
            }

            // 2. Auf alle ausgewählten Dateien anwenden
            foreach (var file in SelectedFiles) {
                if (file.Mp3File != null) {
                    file.Mp3File.AlbumArtist = CurrentArtists.ToString(); 
                    file.Mp3File.Save();
                }

                file.Id3ArtistList = CurrentArtists.ToList();
            }

            OnApplyCompleted?.Invoke(this, EventArgs.Empty);
        } catch (Exception ex) {
            Console.WriteLine($"Fehler beim Hinzufügen der Künstler: {ex.Message}");
        }
    }

    private void Cancel() {
        OnCancel?.Invoke(this, EventArgs.Empty);
    }

    private void UpdatePreview() {
        PreviewItems.Clear();

        foreach (var file in SelectedFiles) {
            var oldArtists = file.Id3ArtistList != null ? string.Join(", ", file.Id3ArtistList) : "Keine Künstler";
            var newArtists = CurrentArtists.Any() ? string.Join(", ",       CurrentArtists) : "(werden entfernt)";

            PreviewItems.Add(new ArtistPreviewItem {
                FileName   = file.FileName,
                OldArtists = oldArtists,
                NewArtists = newArtists
            });
        }
    }

    #endregion

    #region Events

    public event EventHandler OnApplyCompleted;
    public event EventHandler OnCancel;

    #endregion
}