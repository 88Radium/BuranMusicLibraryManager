using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.SQLite;

public partial class DatabaseTable_GenreNames : ObservableObject {
    [ObservableProperty] private int    _ID;
    [ObservableProperty] private string _genreName = "";

    public DatabaseTable_GenreNames() { }

    public DatabaseTable_GenreNames(DataRow row) {
        ID        = Convert.ToInt32(row["ID"]);
        GenreName = Convert.ToString(row["GenreName"]) ?? "";
    }
}