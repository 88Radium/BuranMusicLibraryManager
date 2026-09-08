using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Buran.Interfaces;
using Buran.Localization;
using Buran.Player.Models;
using Buran.Player.Services;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Buran.Player.ViewModels;

public partial class PlayerTabViewModel : ViewModelBase, IPlaybackController, IPlaylistSink {
    private readonly PlaybackEngine     _engine      = new();
    private readonly SpectrogramSource  _spectrogram = new();
    private readonly DispatcherTimer    _timer;
    private bool _folderQueueMode;
    private int  _queueIndex = -1;
    private List<string> _queue = [];

    public PlayerTabViewModel() {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _timer.Tick += (_, _) => OnTick();
        _engine.EndReached       += (_, _) => Dispatcher.UIThread.Post(OnTrackEnded);
        _engine.EncounteredError += (_, _) => Dispatcher.UIThread.Post(() => StatusText = L.Get("Player.PlaybackError"));
        LoadPlaylistsFromDisk();
        L.WhenChanged(() => {
            OnPropertyChanged(nameof(PlayPauseLabel));
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(PositionText));
            OnPropertyChanged(nameof(RepeatModeTip));
        });
    }

    public ObservableCollection<PlaylistTrack> FolderTracks   { get; } = [];
    public ObservableCollection<Playlist>      Playlists      { get; } = [];
    public ObservableCollection<PlaylistTrack> PlaylistTracks { get; } = [];

    [ObservableProperty] private PlaylistTrack? _selectedFolderTrack;
    [ObservableProperty] private Playlist?      _selectedPlaylist;
    [ObservableProperty] private PlaylistTrack? _selectedPlaylistTrack;
    [ObservableProperty] private PlaylistTrack? _nowPlaying;
    [ObservableProperty] private bool           _isPlaying;
    [ObservableProperty] private double         _position;
    [ObservableProperty] private double         _volume = 80;
    [ObservableProperty] private string         _statusText = "";
    [ObservableProperty] private Bitmap?        _spectrogramImage;
    [ObservableProperty] private string?        _currentFolderPath;
    [ObservableProperty] private bool           _isDocked;
    [ObservableProperty] private bool           _isPlaylistOpen;
    [ObservableProperty] private RepeatMode     _repeatMode = RepeatMode.All;

    public GridLength ChromeRowHeight   => IsDocked ? new GridLength(0) : new GridLength(220);
    public GridLength SplitterRowHeight => IsDocked ? new GridLength(0) : new GridLength(6);

    partial void OnIsDockedChanged(bool value) {
        OnPropertyChanged(nameof(ChromeRowHeight));
        OnPropertyChanged(nameof(SplitterRowHeight));
    }

    [RelayCommand]
    private void TogglePlaylistPanel() => IsPlaylistOpen = !IsPlaylistOpen;

    [ObservableProperty] private double _playlistPanelWidth = 280;

    public bool IsRepeatOff  => RepeatMode == RepeatMode.Off;
    public bool IsRepeatAll  => RepeatMode == RepeatMode.All;
    public bool IsRepeatOne  => RepeatMode == RepeatMode.One;
    public bool IsPlayOnce   => RepeatMode == RepeatMode.Once;

    public string RepeatModeTip => RepeatMode switch {
        RepeatMode.All  => L.Get("Player.RepeatAllTip"),
        RepeatMode.One  => L.Get("Player.RepeatOneTip"),
        RepeatMode.Once => L.Get("Player.PlayOnceTip"),
        _               => L.Get("Player.RepeatOffTip")
    };

    [RelayCommand]
    private void CycleRepeatMode() {
        RepeatMode = RepeatMode switch {
            RepeatMode.Off  => RepeatMode.All,
            RepeatMode.All  => RepeatMode.One,
            RepeatMode.One  => RepeatMode.Once,
            _               => RepeatMode.Off
        };
    }

    partial void OnRepeatModeChanged(RepeatMode value) {
        OnPropertyChanged(nameof(IsRepeatOff));
        OnPropertyChanged(nameof(IsRepeatAll));
        OnPropertyChanged(nameof(IsRepeatOne));
        OnPropertyChanged(nameof(IsPlayOnce));
        OnPropertyChanged(nameof(RepeatModeTip));
    }

    public string PlayPauseLabel => IsPlaying ? L.Get("Player.Pause") : L.Get("Player.Play");
    public string PositionText   => FormatClock(_engine.TimeMs) + " / " + FormatClock(_engine.LengthMs);
    public double DurationSeconds => Math.Max(0, _engine.LengthMs / 1000.0);
    public double NyquistHz =>
        NowPlaying is { NyquistHz: > 0 } track ? track.NyquistHz : 22050;
    public bool   HasFolderTracks => FolderTracks.Count > 0;
    public bool   HasPlaylists    => Playlists.Count > 0;
    public bool   HasPlaylistTracks => PlaylistTracks.Count > 0;

    public string SelectedPlaylistName {
        get => SelectedPlaylist?.Name ?? "";
        set {
            if (SelectedPlaylist is null || value is null)
                return;
            if (SelectedPlaylist.Name == value)
                return;
            SelectedPlaylist.Name = value;
            PersistPlaylists();
        }
    }

    public void LoadMusicFolder(string folderPath, bool includeSubfolders = false) {
        CurrentFolderPath = folderPath;
        FolderTracks.Clear();
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) {
            OnPropertyChanged(nameof(HasFolderTracks));
            return;
        }

        IEnumerable<string> files = includeSubfolders
            ? EnumerateAudioRecursive(folderPath)
            : Directory.EnumerateFiles(folderPath);

        foreach (var path in files
                     .Where(p => p.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
                                 || p.EndsWith(".flac", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase))
            FolderTracks.Add(new PlaylistTrack(path));

        OnPropertyChanged(nameof(HasFolderTracks));

        if (_folderQueueMode && NowPlaying is not null &&
            FolderTracks.All(t => !PathsEqual(t.Path, NowPlaying.Path)))
            StopPlayback();
    }

    private static IEnumerable<string> EnumerateAudioRecursive(string root) {
        var stack = new Stack<string>();
        stack.Push(root);
        while (stack.Count > 0) {
            var dir = stack.Pop();
            IEnumerable<string> files = [];
            IEnumerable<string> subs  = [];
            try { files = Directory.EnumerateFiles(dir); } catch { /* skip */ }
            foreach (var file in files)
                yield return file;
            try { subs = Directory.EnumerateDirectories(dir); } catch { /* skip */ }
            foreach (var sub in subs)
                stack.Push(sub);
        }
    }

    public void PlayFile(string path, IReadOnlyList<string>? queue = null) {
        _folderQueueMode = queue is { Count: > 0 };
        _queue = (queue ?? [path]).ToList();
        _queueIndex = Math.Max(0, _queue.FindIndex(p => PathsEqual(p, path)));
        _ = StartAsync(path);
    }

    public void StopPlayback() {
        _engine.Stop();
        _timer.Stop();
        _spectrogram.Clear();
        SpectrogramImage = null;
        IsPlaying        = false;
        NowPlaying       = null;
        Position         = 0;
        OnPropertyChanged(nameof(PlayPauseLabel));
        OnPropertyChanged(nameof(PositionText));
    }

    public void AddTracks(IReadOnlyList<string> paths) {
        if (paths.Count == 0)
            return;

        if (SelectedPlaylist is null) {
            var created = new Playlist { Name = L.Get("Player.DefaultPlaylist") };
            Playlists.Add(created);
            SelectedPlaylist = created;
            OnPropertyChanged(nameof(HasPlaylists));
        }

        foreach (var path in paths) {
            if (SelectedPlaylist.Paths.Any(p => PathsEqual(p, path)))
                continue;
            SelectedPlaylist.Paths.Add(path);
        }

        ReloadPlaylistTracks();
        PersistPlaylists();
    }

    [RelayCommand]
    private void PlayPause() {
        if (NowPlaying is null) {
            var track = SelectedPlaylistTrack ?? SelectedFolderTrack ?? FolderTracks.FirstOrDefault();
            if (track is not null)
                PlayTrack(track, fromPlaylist: SelectedPlaylistTrack is not null);
            return;
        }

        if (!_engine.TryInitialize(out var error)) {
            StatusText = L.Format("Player.LibVlcMissing", error ?? "");
            return;
        }

        _engine.Pause();
        IsPlaying = _engine.IsPlaying;
        OnPropertyChanged(nameof(PlayPauseLabel));
    }

    [RelayCommand]
    private void Stop() => StopPlayback();

    [RelayCommand]
    private void PlayPrevious() {
        if (_queue.Count == 0)
            return;
        _queueIndex = (_queueIndex - 1 + _queue.Count) % _queue.Count;
        _ = StartAsync(_queue[_queueIndex]);
    }

    [RelayCommand]
    private void PlayNext() => Advance(userRequested: true);

    private void OnTrackEnded() => Advance(userRequested: false);

    private void Advance(bool userRequested) {
        if (_queue.Count == 0)
            return;

        if (!userRequested) {
            if (RepeatMode == RepeatMode.One) {
                _ = StartAsync(_queue[_queueIndex]);
                return;
            }

            if (RepeatMode == RepeatMode.Once) {
                StopPlayback();
                return;
            }
        }

        var next = _queueIndex + 1;
        if (next >= _queue.Count) {
            if (userRequested || RepeatMode == RepeatMode.All)
                next = 0;
            else {
                StopPlayback();
                return;
            }
        }

        _queueIndex = next;
        _ = StartAsync(_queue[_queueIndex]);
    }

    [RelayCommand]
    private void PlayFolderTrack(PlaylistTrack? track) {
        if (track is null)
            return;
        PlayTrack(track, fromPlaylist: false);
    }

    [RelayCommand]
    private void PlayPlaylistTrack(PlaylistTrack? track) {
        if (track is null)
            return;
        PlayTrack(track, fromPlaylist: true);
    }

    [RelayCommand]
    private void AddFolderSelectionToPlaylist() {
        if (SelectedFolderTrack is not null)
            AddTracks([SelectedFolderTrack.Path]);
        else
            AddTracks(FolderTracks.Select(t => t.Path).ToList());
    }

    [RelayCommand]
    private void RemovePlaylistTrack() {
        if (SelectedPlaylist is null || SelectedPlaylistTrack is null)
            return;
        SelectedPlaylist.Paths.RemoveAll(p => PathsEqual(p, SelectedPlaylistTrack.Path));
        ReloadPlaylistTracks();
        PersistPlaylists();
    }

    [RelayCommand]
    private void NewPlaylist() {
        var playlist = new Playlist { Name = UniquePlaylistName() };
        Playlists.Add(playlist);
        SelectedPlaylist = playlist;
        OnPropertyChanged(nameof(HasPlaylists));
        PersistPlaylists();
    }

    [RelayCommand]
    private async Task DeletePlaylist() {
        if (SelectedPlaylist is null)
            return;
        if (!await BuranMessageBox.AskYesNo(L.Format("Player.DeletePlaylistConfirm", SelectedPlaylist.Name)))
            return;
        Playlists.Remove(SelectedPlaylist);
        SelectedPlaylist = Playlists.FirstOrDefault();
        OnPropertyChanged(nameof(HasPlaylists));
        PersistPlaylists();
    }

    [RelayCommand]
    private async Task ImportM3u() {
        var path = await FilePickers.OpenM3uAsync();
        if (string.IsNullOrEmpty(path))
            return;

        var tracks = M3uService.Read(path);
        var playlist = new Playlist {
            Name  = Path.GetFileNameWithoutExtension(path),
            Paths = tracks
        };
        Playlists.Add(playlist);
        SelectedPlaylist = playlist;
        OnPropertyChanged(nameof(HasPlaylists));
        PersistPlaylists();
    }

    [RelayCommand]
    private async Task ExportM3u() {
        if (SelectedPlaylist is null || PlaylistTracks.Count == 0)
            return;
        var path = await FilePickers.SaveM3uAsync(SelectedPlaylist.Name + ".m3u");
        if (string.IsNullOrEmpty(path))
            return;
        M3uService.Write(path, PlaylistTracks.Select(t => (t.Path, t.DisplayTitle, t.DurationSeconds)));
    }

    partial void OnSelectedPlaylistChanged(Playlist? value) {
        ReloadPlaylistTracks();
        OnPropertyChanged(nameof(SelectedPlaylistName));
        PersistPlaylists();
    }

    partial void OnVolumeChanged(double value) {
        if (_engine.TryInitialize(out _))
            _engine.Volume = (int)Math.Round(value);
    }

    public void SeekTo(double position) {
        Position = Math.Clamp(position, 0, 1);
        if (_engine.LengthMs > 0)
            _engine.Position = (float)Position;
    }

    private void PlayTrack(PlaylistTrack track, bool fromPlaylist) {
        if (fromPlaylist) {
            _folderQueueMode = false;
            _queue = PlaylistTracks.Select(t => t.Path).ToList();
        }
        else {
            _folderQueueMode = true;
            _queue = FolderTracks.Select(t => t.Path).ToList();
        }

        _queueIndex = Math.Max(0, _queue.FindIndex(p => PathsEqual(p, track.Path)));
        _ = StartAsync(track.Path);
    }

    private async Task StartAsync(string path) {
        if (!_engine.TryInitialize(out var error)) {
            StatusText = L.Format("Player.LibVlcMissing", error ?? "");
            return;
        }

        if (!File.Exists(path)) {
            StatusText = L.Format("Player.MissingFile", Path.GetFileName(path));
            return;
        }

        if (!_engine.Play(path, out error)) {
            StatusText = error ?? L.Get("Player.PlaybackError");
            return;
        }

        _engine.Volume = (int)Math.Round(Volume);
        var playing    = new PlaylistTrack(path);
        playing.EnsureSampleRate();
        NowPlaying     = playing;
        IsPlaying      = true;
        StatusText     = L.Get("Player.ComputingSpectrum");
        _timer.Start();
        OnPropertyChanged(nameof(PlayPauseLabel));

        var stopHz = (int)Math.Round(playing.NyquistHz);
        var image = await _spectrogram.LoadAsync(path, stopHz);
        if (NowPlaying is not null && PathsEqual(NowPlaying.Path, path)) {
            SpectrogramImage = image;
            if (StatusText == L.Get("Player.ComputingSpectrum"))
                StatusText = image is null ? L.Get("Player.SpectrumFailed") : "";
        }
    }

    partial void OnNowPlayingChanged(PlaylistTrack? value) =>
        OnPropertyChanged(nameof(NyquistHz));

    private void OnTick() {
        IsPlaying = _engine.IsPlaying;
        if (_engine.LengthMs > 0)
            Position = _engine.Position;

        OnPropertyChanged(nameof(PositionText));
        OnPropertyChanged(nameof(PlayPauseLabel));
        OnPropertyChanged(nameof(DurationSeconds));
        OnPropertyChanged(nameof(NyquistHz));

        if (NowPlaying is not null && !File.Exists(NowPlaying.Path))
            StopPlayback();
    }

    private void ReloadPlaylistTracks() {
        PlaylistTracks.Clear();
        if (SelectedPlaylist is not null) {
            foreach (var path in SelectedPlaylist.Paths)
                PlaylistTracks.Add(new PlaylistTrack(path));
        }

        OnPropertyChanged(nameof(HasPlaylistTracks));
    }

    private void LoadPlaylistsFromDisk() {
        var data = PlaylistStore.Load();
        Playlists.Clear();
        foreach (var dto in data.Playlists) {
            Playlists.Add(new Playlist {
                Id    = string.IsNullOrEmpty(dto.Id) ? Guid.NewGuid().ToString("N") : dto.Id,
                Name  = string.IsNullOrWhiteSpace(dto.Name) ? L.Get("Player.DefaultPlaylist") : dto.Name,
                Paths = dto.Paths ?? []
            });
        }

        SelectedPlaylist = Playlists.FirstOrDefault(p => p.Id == data.SelectedId) ?? Playlists.FirstOrDefault();
        OnPropertyChanged(nameof(HasPlaylists));
    }

    private void PersistPlaylists() =>
        PlaylistStore.Save(Playlists, SelectedPlaylist?.Id);

    private string UniquePlaylistName() {
        var n = 1;
        string name;
        do {
            name = L.Format("Player.NewPlaylistName", n);
            n++;
        } while (Playlists.Any(p => p.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase)));

        return name;
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);

    private static string FormatClock(long ms) {
        if (ms <= 0)
            return "0:00";
        var t = TimeSpan.FromMilliseconds(ms);
        return t.ToString(t.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss");
    }
}
