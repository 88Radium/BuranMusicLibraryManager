namespace Buran.ID3Editor.Types;

public class ArtistPreviewItem {
    public string FileName   { get; set; }
    public string OldArtists { get; set; }
    public string NewArtists { get; set; }

    public override string ToString() {
        return $"{FileName}: {OldArtists} → {NewArtists}";
    }
}