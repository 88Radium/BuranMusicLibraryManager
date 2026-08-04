using Buran.Interfaces;
namespace Buran.SQLite;

public class DbArtistNameResolver : IArtistNameResolver {
    public ArtistResolutionResult ResolveArtistName(string artistName) {
        var result = DBConnector.CheckForPreferredName(artistName);

        return new ArtistResolutionResult {
            PreferredName = result.PreferredArtistName,
            Status        = result.ArtistNameStatus,
        };
    }

    public void SaveNewArtist(string artistName, string realName = "") {
        DBConnector.InsertInto_ArtistNames(artistName, realName);
    }
}