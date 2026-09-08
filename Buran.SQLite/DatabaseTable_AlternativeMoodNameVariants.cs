using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.SQLite;

public partial class DatabaseTable_AlternativeMoodNameVariants : ObservableObject {
    [ObservableProperty] private int    _id;
    [ObservableProperty] private string _moodNameVariant = "";
    [ObservableProperty] private int    _isMissSpelled;
    [ObservableProperty] private int    _refersToMoodName;

    public DatabaseTable_AlternativeMoodNameVariants() { }

    public DatabaseTable_AlternativeMoodNameVariants(DataRow row) {
        Id               = Convert.ToInt32(row["ID"]);
        MoodNameVariant  = Convert.ToString(row["MoodNameVariant"]) ?? "";
        IsMissSpelled    = row["IsMissSpelled"] is bool flag ? (flag ? 1 : 0) : Convert.ToInt32(row["IsMissSpelled"]);
        RefersToMoodName = Convert.ToInt32(row["RefersToMoodName"]);
    }
}
