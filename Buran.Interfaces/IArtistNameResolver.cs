using Buran.Types;

namespace Buran.Interfaces {
    public interface IArtistNameResolver {
        /// <summary>
        /// Findet den bevorzugten Künstlernamen für einen gegebenen Namen
        /// </summary>
        ArtistResolutionResult ResolveArtistName(string artistName);

        /// <summary>
        /// Speichert einen neuen Künstler in der DB
        /// </summary>
        void SaveNewArtist(string artistName, string realName = "");
    }

    public class ArtistResolutionResult {
        public string?          PreferredName { get; set; }
        public ArtistNameStatus Status        { get; set; }
        public bool             IsNew         => Status == ArtistNameStatus.IsNonExistent;
    }
}