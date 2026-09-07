using System.Data;
using Buran.Localization;
using Buran.Types;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.SQLite;

public partial class DatabaseTable_FeatureKeywords : ObservableObject {
    [ObservableProperty] private int    _id;
    [ObservableProperty] private string _keyword = "";
    [ObservableProperty] private string _type    = CollaborationMarkers.TypeCollaboration;
    [ObservableProperty] private int    _weight  = 5;

    public DatabaseTable_FeatureKeywords() { }

    public DatabaseTable_FeatureKeywords(DataRow row) {
        Id      = Convert.ToInt32(row["Id"]);
        Keyword = Convert.ToString(row["Keyword"]) ?? "";
        Type    = Convert.ToString(row["Type"]) ?? CollaborationMarkers.TypeCollaboration;
        Weight  = row["Weight"] is DBNull ? 5 : Convert.ToInt32(row["Weight"]);
    }

    public string TypeLabel => CollaborationMarkers.IsVersion(Type)
        ? L.Get("Db.KeywordTypeVersion")
        : L.Get("Db.KeywordTypeCollaboration");

    public void NotifyTypeLabel() => OnPropertyChanged(nameof(TypeLabel));

    partial void OnTypeChanged(string value) => NotifyTypeLabel();
}
