using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.SQLite;

public partial class DatabaseTable_MoodNames : ObservableObject {
    [ObservableProperty] private int    _ID;
    [ObservableProperty] private string _moodName = "";

    public DatabaseTable_MoodNames() { }

    public DatabaseTable_MoodNames(DataRow row) {
        ID       = Convert.ToInt32(row["ID"]);
        MoodName = Convert.ToString(row["MoodName"]) ?? "";
    }
}