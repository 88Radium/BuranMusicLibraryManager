using Avalonia.Controls;

namespace Buran.Interfaces;

/// <summary>
/// Player module surface: transport + spectrogram + playlists, hosted by the shell.
/// </summary>
public interface IPlayerDock {
    /// <summary>
    /// Dock strip (visualizer + optional playlist). The player tab is not added when this is used.
    /// </summary>
    Control TakeDock();
}
