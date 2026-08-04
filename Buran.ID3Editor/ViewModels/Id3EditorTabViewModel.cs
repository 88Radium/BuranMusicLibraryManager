using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Buran.Interfaces;
using Buran.SQLite;
using CommunityToolkit.Mvvm.Input;
using Buran.ID3Editor.Views;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;


namespace Buran.ID3Editor.ViewModels;

public partial class Id3EditorTabViewModel : ViewModelBase {
    private readonly IStorageProvider? _storageProvider;

    /// <summary>
    /// Constructor: Sets up Commands and NotifyPropertyChanged
    /// </summary>
    public Id3EditorTabViewModel(IStorageProvider? storageProvider = null) {
        _storageProvider            = storageProvider;
        _selectedPath               = string.Empty;
        _musicFiles                 = null;
        _openAddArtistDialogCommand = null;
        OfdCommand                  = new RelayCommand(OpenMusicFolderDialog);

        // FileNameFromId3 uses async/await for the BuranMessageBox. Dafuq AI suggested to make it async. Who knows how to fix that. Too many construction sites at once.
        FileNameFromId3Command       =  new RelayCommand<object>(FileNameFromId3);
        Id3FromFileNameCommand       =  new RelayCommand<object>(Id3FromFileName!);
        ResetId3ToDefaultCommand     =  new RelayCommand<object>(ResetID3ToDefault!);
        MusicFiles                   =  new ObservableCollection<Buran.Types.Mp3FileObject>();
        MusicFiles.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasMusicFiles));
        MusicFiles.CollectionChanged += OnMusicFilesCollectionChanged!;
    }


    #region PropertyChanged

    private void OnMusicFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        // Neue Files: PropertyChanged für IsSelected abonnieren
        if (e.NewItems != null) {
            foreach (Mp3FileObject file in e.NewItems) {
                file.PropertyChanged += OnFilePropertyChanged;
            }
        }

        // Entfernte Files: Event abbestellen
        if (e.OldItems != null) {
            foreach (Mp3FileObject file in e.OldItems) {
                file.PropertyChanged -= OnFilePropertyChanged;
            }
        }

        OnPropertyChanged(nameof(HasSelectedFiles));
    }

    private void OnFilePropertyChanged(object sender, PropertyChangedEventArgs e) {
        if (e.PropertyName != nameof(Mp3FileObject.IsSelected)) return;
        OnPropertyChanged(nameof(HasSelectedFiles));
        OnPropertyChanged(nameof(SelectedFilesCount));
    }

    #endregion


    #region OpenFolderDialog

    public ICommand OfdCommand { get; set; }

    [ObservableProperty] private string _selectedPath;

    private ObservableCollection<Mp3FileObject>? _musicFiles;

    public ObservableCollection<Mp3FileObject> MusicFiles {
        get => _musicFiles;
        set {
            _musicFiles = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMusicFiles));
        }
    }

    public bool HasMusicFiles => MusicFiles?.Count > 0;


    public async void OpenMusicFolderDialog() {
        if (_storageProvider is null) {
            await BuranMessageBox.Show("StorageProvider ist nicht verfügbar.");
            return;
        }

        var options = new FolderPickerOpenOptions {
            Title         = "Bitte wähle einen Ordner aus",
            AllowMultiple = false // true für Mehrfachauswahl
        };

        var folders = await _storageProvider.OpenFolderPickerAsync(options);

        if (folders.Count > 0) {
            var selectedFolder = folders[0];
            var path           = selectedFolder.Path.AbsolutePath;
            // Verwende den Pfad
        }
    }

    #endregion


    #region LoadMusicFiles

    public void LoadMusicFiles() {
        MusicFiles.Clear();

        // Select all MP3 Files in Folder
        if (SelectedPath == string.Empty) return;
        if (!Directory.Exists(SelectedPath)) return;

        string[] filePaths = System.IO.Directory.GetFiles(SelectedPath);
        filePaths = filePaths.Where(x => x.EndsWith(".mp3") || x.EndsWith(".flac")).ToArray();

        List<string> pathsOfMusicFilesToDisplay = new List<string>();
        foreach (string filePath in filePaths) {
            switch (filePath.Substring(filePath.LastIndexOf("."))) {
                case ".flac":
                case ".mp3":
                    // case ".dsf":
                    pathsOfMusicFilesToDisplay.Add(filePath);
                    break;
            }
        }

        foreach (string path in pathsOfMusicFilesToDisplay) {
            Mp3FileObject mp3 = new Mp3FileObject(path);
            MusicFiles.Add(mp3);

            if (mp3.Id3Artists != null && mp3.Id3Artists.Count() > 0) {
                foreach (string artist in mp3.Id3Artists) {
                    EventPublisher.PublishNewArtist(artist);
                }
            }
        }
    }

    #endregion


    #region ID3 To Filename

    public ICommand FileNameFromId3Command { get; set; }

    /// <summary>
    /// Should just take all Artists from the ID3-Tag and inserts them into the file name.
    /// Checks like for preferred names and similar stuff is not part of this method.
    /// </summary>
    /// <param name="sender"></param>
    public void FileNameFromId3(object sender) {
        Mp3FileObject sndr            = (Mp3FileObject)sender;
        string        artistString    = GetArtistNamesFromId3AsFormattedString(sndr);
        string        SongTitleString = GetSongTitleFromId3(sndr);

        if (string.IsNullOrEmpty(artistString)) {
            artistString = GetArtistsFromFileName();
        }

        string OriginalFileName = sndr.FileName;


        // Sets new Filename
        sndr.FileName = artistString + " - " + SongTitleString + "." + sndr.FileType;
        try {
            // Saves Any Changes To The MetaData
            sndr.TagLibFile.Save();

            // Saves The File Under The New Filename
            try {
                System.IO.File.Move(Path.Combine(sndr.ContainingDirectoryName, OriginalFileName),
                    Path.Combine(sndr.ContainingDirectoryName,                 sndr.FileName));
            }
            catch (IOException IOex) {
                //TODO: Datei-Ersetzen-Dialog - Option zum Überschreiben/Überspringen hinzufügen.
                BuranMessageBox.Show($"Es existiert bereits eine Datei mit demselben Namen:\n{IOex.Message}").Wait();
            }


            /// Replaces The Mp3FileObject In The ListView.ItemsSource With The One Saved Under The New Filename.
            /// Otherwise there will be an Exception in [sndr.File.Save();] as soon as you want to save a second change in a row
            /// because of the sndr.File points to the Filepath it had as it was loaded initially. File.Save() will throw an exception then.
            MusicFiles[MusicFiles.IndexOf(MusicFiles.First(x => x.FileName == sndr.FileName))] =
                new Mp3FileObject(Path.Combine(sndr.ContainingDirectoryName, sndr.FileName));
        }
        catch (IndexOutOfRangeException) {
            BuranMessageBox.Show(
                "The existing Filename could not be found in the List of music files to display.").Wait();
        }
        catch (NullReferenceException) {
            BuranMessageBox.Show(
                "The displayed object could not be saved, because the internal path to the file on the file system was not found." +
                "This can be casued by an unsuccessful saving-attempt or may be unvalid characters in the file name."
            ).Wait();
        }
    }


    /// <summary>
    /// Pulls the title from the ID3 tags and returns it
    /// </summary>
    /// <param name="FileObject"></param>
    /// <returns>[string]</returns>
    private string GetSongTitleFromId3(Mp3FileObject FileObject) {
        return FileObject.Id3Title;
    }


    /// <summary>
    /// pulls all artists from the ID3 tags and puts them in a format that is filename friendly.
    /// </summary>
    /// <param name="fileObject"></param>
    /// <returns>[string] with all Artists, formatted in a filename friendly way</returns>
    public string GetArtistNamesFromId3AsFormattedString(Mp3FileObject fileObject) {
        List<string> PreferredArtistNameList = new List<string>();
        var          sb                      = new StringBuilder();

        // Assembling ArtistListing
        foreach (string artist in fileObject.Id3Artists) {
            string preferred = DBConnector.CheckForPreferredName(artist).PreferredArtistName;
            PreferredArtistNameList.Add(preferred);
        }


        // from here, work with preferredArtistNameList!
        if (PreferredArtistNameList.Count == 2) {
            // Nur 2 Artists: "Artist1 feat. Artist2"
            sb.Append(PreferredArtistNameList[0]);
            sb.Append(" feat. ");
            sb.Append(PreferredArtistNameList[1]);
        }
        else if (PreferredArtistNameList.Count > 2) {
            // Mehr als 2 Artists
            for (int i = 0; i < PreferredArtistNameList.Count; i++) {
                sb.Append(PreferredArtistNameList[i]);

                if (i < PreferredArtistNameList.Count - 1) {
                    if (i == 0)
                        sb.Append(" feat. "); // Erster zu Zweitem
                    else if (i == PreferredArtistNameList.Count - 2)
                        sb.Append(" & "); // Vorletzter zu Letztem
                    else
                        sb.Append(", "); // Alle dazwischen
                }
            }
        }
        else if (PreferredArtistNameList.Count == 1) {
            sb.Append(PreferredArtistNameList[0]);
        }

        return sb.ToString();
    }

    #endregion


    #region Filename To ID3

    public ICommand Id3FromFileNameCommand { get; set; }

    public void Id3FromFileName(object sender) {
        ArgumentNullException.ThrowIfNull(sender);
        Mp3FileObject sndr = sender as Mp3FileObject;
        if (sndr == null) return;

        var parser   = new FileNameParser();
        var metadata = parser.Parse(sndr.FileName);

        // NUR ID3-Properties setzen
        if (!string.IsNullOrEmpty(metadata.Title)) {
            sndr.Id3Title = metadata.Title;
        }

        if (metadata.Artists != null && metadata.Artists.Any()) {
            sndr.Id3Artists = metadata.Artists.ToArray();
        }

        sndr.SaveTags();
    }

    public string GetTitleFromFileName() {
        return string.Empty;
    }


    public string GetArtistsFromFileName() {
        return string.Empty;
    }

    public string GetAlbumFromFileName() {
        return string.Empty;
    }

    public string GetTitleNumberFromFileName() {
        return string.Empty;
    }

    #endregion


    #region ResetToDefaultValues

    public ICommand ResetId3ToDefaultCommand { get; set; }

    public void ResetID3ToDefault(object sender) {
        ArgumentNullException.ThrowIfNull(sender);
        Mp3FileObject sndr = (Mp3FileObject)sender;
        sndr.TagLibFile = sndr.FileInInitialState;
    }

    #endregion


    #region Bulk_AddArtist

    private ICommand _openAddArtistDialogCommand;

    public ICommand OpenAddArtistDialogCommand => _openAddArtistDialogCommand ??= new RelayCommand(OpenAddArtistDialog);

    public bool HasSelectedFiles   => MusicFiles?.Any(f => f.IsSelected    == true) ?? false;
    public int  SelectedFilesCount => MusicFiles?.Count(f => f?.IsSelected == true) ?? 0;

    // In ID3EditorTabViewModel.cs
    private void OpenAddArtistDialog() {
        var selectedFiles = MusicFiles.Where(f => f.IsSelected).ToList();
        if (!selectedFiles.Any()) return;

        var dialog = new AddArtistToID3DialogWindow();
        var vm     = new AddArtistToID3DialogWindowViewModel(selectedFiles);
        dialog.DataContext = vm;

        vm.OnApplyCompleted += (s, e) => {
            dialog.Close();
            OnPropertyChanged(nameof(MusicFiles));
        };

        vm.OnCancel += (s, e) => dialog.Close();


        var owner = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

        // 5. Dialog mit Owner anzeigen (async/await)
        if (owner != null) {
            dialog.ShowDialog(owner);
        }
    }

    #endregion
}