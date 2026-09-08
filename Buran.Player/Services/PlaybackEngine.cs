using System.Diagnostics;
using LibVLCSharp.Shared;

namespace Buran.Player.Services;

public sealed class PlaybackEngine : IDisposable {
    private LibVLC?      _lib;
    private MediaPlayer? _player;
    private bool         _initialized;
    private string?      _initError;

    public event EventHandler? EndReached;
    public event EventHandler? EncounteredError;

    public bool IsPlaying => _player?.IsPlaying == true;
    public bool CanSeek   => _player?.IsSeekable == true;

    public long TimeMs {
        get => _player?.Time ?? 0;
        set {
            if (_player is not null)
                _player.Time = Math.Max(0, value);
        }
    }

    public long LengthMs => Math.Max(0, _player?.Length ?? 0);

    public float Position {
        get => _player?.Position ?? 0;
        set {
            if (_player is not null)
                _player.Position = Math.Clamp(value, 0f, 1f);
        }
    }

    public int Volume {
        get => _player?.Volume ?? 0;
        set {
            if (_player is not null)
                _player.Volume = Math.Clamp(value, 0, 100);
        }
    }

    public bool TryInitialize(out string? error) {
        if (_initialized) {
            error = _initError;
            return _player is not null;
        }

        _initialized = true;
        try {
            NativeLibVlc.Prepare();
            _lib    = new LibVLC(NativeLibVlc.LibVlcOptions());
            _player = new MediaPlayer(_lib);
            _player.EndReached       += (_, _) => EndReached?.Invoke(this, EventArgs.Empty);
            _player.EncounteredError += (_, _) => EncounteredError?.Invoke(this, EventArgs.Empty);
            _player.Volume = 80;
            error = null;
            return true;
        }
        catch (Exception ex) {
            _initError = ex.Message;
            error      = ex.Message;
            Debug.WriteLine($"LibVLC init failed: {ex}");
            return false;
        }
    }

    public bool Play(string path, out string? error) {
        error = null;
        if (!TryInitialize(out error) || _lib is null || _player is null)
            return false;

        try {
            using var media = new Media(_lib, path, FromType.FromPath);
            return _player.Play(media);
        }
        catch (Exception ex) {
            error = ex.Message;
            return false;
        }
    }

    public void Pause()  => _player?.Pause();
    public void Stop()   => _player?.Stop();

    public void Dispose() {
        _player?.Dispose();
        _lib?.Dispose();
        _player = null;
        _lib    = null;
    }

}
