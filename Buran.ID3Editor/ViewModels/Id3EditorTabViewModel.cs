using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    /// <summary>
    /// Constructor: Sets up Commands and NotifyPropertyChanged
    /// </summary>
    public Id3EditorTabViewModel() {
        _selectedPath               = string.Empty;
        _musicFiles                 = null;
        _openAddArtistDialogCommand = null;
        
        // ✅ Wrap den async-Call in einen RelayCommand
        OfdCommand = new RelayCommand(async () => await OpenMusicFolderDialog(), () => true);

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

    public ICommand             OfdCommand      { get; set; }
    public Func<Task<string?>>? PickFolderAsync { get; set; }

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

    // ✅ Robust Cross-Platform Folder Browser Dialog with StorageProvider + OpenFolderDialog fallback
    public async Task<string?> BrowseFolder() {
        try {
            // Try to obtain a TopLevel from the Avalonia Application lifetime (classic desktop)
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            TopLevel? tl = lifetime?.MainWindow;

            if (tl is null) {
                // As an extra fallback, try first window in lifetime
                tl = lifetime?.Windows?.FirstOrDefault();
            }

            if (tl is null) {
                Debug.WriteLine("❌ No TopLevel/MainWindow available to show dialogs");
            } else {
                // First attempt: StorageProvider (recommended on Linux/Wayland/Flatpak etc.)
                try {
                    var provider = tl.StorageProvider;
                    if (provider != null) {
                        Debug.WriteLine("🔎 Trying StorageProvider.OpenFolderPickerAsync");
                        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Musikordner auswählen", AllowMultiple = false });
                        if (folders?.Count > 0) {
                            var p = folders[0].Path.AbsolutePath;
                            Debug.WriteLine($"✅ StorageProvider result: {p}");
                            return p;
                        }
                        Debug.WriteLine("ℹ️ StorageProvider returned nothing or was cancelled");
                    } else {
                        Debug.WriteLine("⚠️ No StorageProvider on TopLevel");
                    }
                } catch (Exception ex) {
                    Debug.WriteLine($"⚠️ StorageProvider failed: {ex.Message}");
                }

                // Second attempt: platform-specific external pickers (Linux: zenity/kdialog)
                try {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                        Debug.WriteLine("🔁 Trying zenity/kdialog fallback (Linux)");

                        // Try zenity
                        try {
                            var zenity = "/usr/bin/zenity";
                            if (File.Exists(zenity)) {
                                var psi = new ProcessStartInfo(zenity, "--file-selection --directory") {
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false
                                };
                                using var p = Process.Start(psi);
                                var outp = await p.StandardOutput.ReadToEndAsync();
                                p.WaitForExit();
                                outp = outp?.Trim();
                                if (!string.IsNullOrEmpty(outp)) {
                                    Debug.WriteLine($"✅ zenity result: {outp}");
                                    return outp;
                                }
                            }
                        } catch (Exception ex) {
                            Debug.WriteLine($"⚠️ zenity failed: {ex.Message}");
                        }

                        // Try kdialog
                        try {
                            var kdialog = "/usr/bin/kdialog";
                            if (File.Exists(kdialog)) {
                                var psi = new ProcessStartInfo(kdialog, "--getexistingdirectory") {
                                    RedirectStandardOutput = true,
                                    UseShellExecute = false
                                };
                                using var p = Process.Start(psi);
                                var outp = await p.StandardOutput.ReadToEndAsync();
                                p.WaitForExit();
                                outp = outp?.Trim();
                                if (!string.IsNullOrEmpty(outp)) {
                                    Debug.WriteLine($"✅ kdialog result: {outp}");
                                    return outp;
                                }
                            }
                        } catch (Exception ex) {
                            Debug.WriteLine($"⚠️ kdialog failed: {ex.Message}");
                        }

                        Debug.WriteLine("ℹ️ No zenity/kdialog result or not installed");
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                        Debug.WriteLine("🔁 macOS fallback not implemented");
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        Debug.WriteLine("🔁 Windows fallback not implemented");
                    }
                } catch (Exception ex) {
                    Debug.WriteLine($"❌ Fallback pickers failed: {ex.Message}");
                }
            }

            return null;
        } catch (Exception ex) {
            Debug.WriteLine($"❌ BrowseFolder error: {ex}");
            return null;
        }
    }


    public async Task OpenMusicFolderDialog() {
        // Use PickFolderAsync if provided by View code-behind, otherwise fallback to BrowseFolder()
        var picker = PickFolderAsync ?? BrowseFolder;

        Debug.WriteLine($"🔎 OpenMusicFolderDialog using picker: {(PickFolderAsync != null ? "PickFolderAsync (code-behind)" : "BrowseFolder (vm)")}");

        if (picker is null) {
            Debug.WriteLine("❌ Kein Folder-Picker verfügbar");
            return;
        }

        var path = await picker();
        Debug.WriteLine($"🔍 Picker returned path: {(path == null ? "<null>" : path)}");

        if (string.IsNullOrEmpty(path)) return;

        SelectedPath = path;
        LoadMusicFiles();
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
    public async void FileNameFromId3(object sender) {
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
            sndr.Mp3File.Save();

            // Saves The File Under The New Filename
            try {
                System.IO.File.Move(Path.Combine(sndr.ContainingDirectoryName, OriginalFileName),
                    Path.Combine(sndr.ContainingDirectoryName,                 sndr.FileName));
            }
            catch (IOException IOex) {
                //TODO: Datei-Ersetzen-Dialog - Option zum Überschreiben/Überspringen hinzufügen.
                await BuranMessageBox.Show($"Es existiert bereits eine Datei mit demselben Namen:\n{IOex.Message}");
            }


            /// Replaces The Mp3FileObject In The ListView.ItemsSource With The One Saved Under The New Filename.
            /// Otherwise there will be an Exception in [sndr.File.Save();] as soon as you want to save a second change in a row
            /// because of the sndr.File points to the Filepath it had as it was loaded initially. File.Save() will throw an exception then.
            MusicFiles[MusicFiles.IndexOf(MusicFiles.First(x => x.FileName == sndr.FileName))] =
                new Mp3FileObject(Path.Combine(sndr.ContainingDirectoryName, sndr.FileName));
        }
        catch (IndexOutOfRangeException) {
            await BuranMessageBox.Show(
                "The existing Filename could not be found in the List of music files to display.");
        }
        catch (NullReferenceException) {
            await BuranMessageBox.Show(
                "The displayed object could not be saved, because the internal path to the file on the file system was not found." +
                "This can be casued by an unsuccessful saving-attempt or may be unvalid characters in the file name."
            );
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
        sndr.Mp3File = sndr.Mp3FileInInitialState;
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