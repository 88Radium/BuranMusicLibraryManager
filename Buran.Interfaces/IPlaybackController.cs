namespace Buran.Interfaces;

/// <summary>
/// Optional MEF contract: play a file (and optional queue) from another module.
/// </summary>
public interface IPlaybackController {
    void PlayFile(string path, IReadOnlyList<string>? queue = null);
    void StopPlayback();
}
