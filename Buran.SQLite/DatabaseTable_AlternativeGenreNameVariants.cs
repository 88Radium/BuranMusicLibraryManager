using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.SQLite;

public partial class DatabaseTable_AlternativeGenreNameVariants : ObservableObject {
    [ObservableProperty] private int    _id;
    [ObservableProperty] private string _genreNameVariant = "";
    [ObservableProperty] private int    _isMissSpelled;
    [ObservableProperty] private int    _refersToGenreName;

    public DatabaseTable_AlternativeGenreNameVariants() { }

    public DatabaseTable_AlternativeGenreNameVariants(DataRow row) {
        Id                 = Convert.ToInt32(row["ID"]);
        GenreNameVariant   = Convert.ToString(row["GenreNameVariant"]) ?? "";
        IsMissSpelled      = row["IsMissSpelled"] is bool flag ? (flag ? 1 : 0) : Convert.ToInt32(row["IsMissSpelled"]);
        RefersToGenreName  = Convert.ToInt32(row["RefersToGenreName"]);
    }
}
