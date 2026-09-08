using System.Data;
using Buran.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.SQLite;

public partial class DatabaseTable_BlockedCatalogValues : ObservableObject {
    public const string KindArtist = "ARTIST";
    public const string KindGenre  = "GENRE";
    public const string KindMood   = "MOOD";

    [ObservableProperty] private int    _id;
    [ObservableProperty] private string _kind  = KindArtist;
    [ObservableProperty] private string _value = "";

    public DatabaseTable_BlockedCatalogValues() { }

    public DatabaseTable_BlockedCatalogValues(DataRow row) {
        Id    = Convert.ToInt32(row["Id"]);
        Kind  = Convert.ToString(row["Kind"]) ?? KindArtist;
        Value = Convert.ToString(row["Value"]) ?? "";
    }

    public string KindLabel => Kind.ToUpperInvariant() switch {
        KindArtist => L.Get("Catalog.KindArtist"),
        KindGenre  => L.Get("Catalog.KindGenre"),
        KindMood   => L.Get("Catalog.KindMood"),
        _          => Kind
    };

    public void NotifyKindLabel() => OnPropertyChanged(nameof(KindLabel));
}
