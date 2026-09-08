namespace Buran.Interfaces;

/// <summary>
/// Optional MEF contract: add tracks to the player playlist from another module.
/// </summary>
public interface IPlaylistSink {
    void AddTracks(IReadOnlyList<string> paths);
}
