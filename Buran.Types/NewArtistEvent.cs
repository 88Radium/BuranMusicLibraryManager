namespace Buran.Types;

public class NewArtistEvent(string artistName, string? realName = null, ArtistNameStatus nameStatus = ArtistNameStatus.IsUnchecked) : EventArgs {
    public string           PreferredArtistName { get; set; } = artistName;
    public string?          RealName            { get; set; } = realName ?? artistName;
    public ArtistNameStatus ArtistNameStatus    { get; set; } = nameStatus;
}
