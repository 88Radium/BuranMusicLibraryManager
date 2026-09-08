namespace Buran.Interfaces;

/// <summary>
/// ID3-editor list: show a set of files (e.g. from a catalog right-click) and index the library.
/// </summary>
public interface ITrackListHost {
    void ShowTracks(IReadOnlyList<string> paths, string caption);
    void IndexLibrary(IReadOnlyList<string> rootPaths);
}
