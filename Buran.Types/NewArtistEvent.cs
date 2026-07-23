using System;

namespace Buran.Types;

public class NewArtistEvent(string artistName, string? realName = null, ArtistNameStatus nameStatus = ArtistNameStatus.IsUnchecked) : EventArgs {
    public string           PreferredArtistName { get; set; } = artistName;
    public string?          RealName            { get; set; } = realName ?? artistName;
    public ArtistNameStatus ArtistNameStatus    { get; set; } = nameStatus;
}

// EventPublisher.cs (einfacher statischer Publisher)
public static class EventPublisher {
    public static event EventHandler<NewArtistEvent>? ArtistDiscovered;

    public static void PublishNewArtist(string preferredArtistName, string? realName = null) {
        ArtistDiscovered?.Invoke(null, new NewArtistEvent(preferredArtistName, realName));
    }
}