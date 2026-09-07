using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.SQLite;

public partial class DatabaseTable_AlternativeGenreNameVariants : ObservableObject {
    [ObservableProperty] private int    _ID;
    [ObservableProperty] private string _GenreNameVariant = "";
    [ObservableProperty] private int    _IsMissSpelled;
    [ObservableProperty] private int    _RefersToGenreName;
}