namespace Buran.Interfaces;

/// <summary>
/// Optional MEF-Tab-Vertrag: die Shell lädt Musikdateien, wenn in der Bibliotheksleiste
/// ein Ordner gewählt wird.
/// </summary>
public interface IMusicFolderConsumer {
    void LoadMusicFolder(string folderPath) => LoadMusicFolder(folderPath, includeSubfolders: false);

    void LoadMusicFolder(string folderPath, bool includeSubfolders);
}
