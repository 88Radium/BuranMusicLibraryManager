namespace Buran.Types;

public readonly record struct CatalogNameResolution(string PreferredName, ArtistNameStatus Status) {
    public bool IsKnown => Status is ArtistNameStatus.AlreadyExisting or ArtistNameStatus.IsAlternativeName;
}
