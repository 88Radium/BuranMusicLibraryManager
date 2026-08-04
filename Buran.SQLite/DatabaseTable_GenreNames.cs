using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.SQLite;

public partial class DatabaseTable_GenreNames : ObservableObject {

    // #region PropertyChanged
    // // : INotifyPropertyChanged
    // public event PropertyChangedEventHandler PropertyChanged;
    // private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
    //     PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    // }
    // #endregion

    [ObservableProperty]
    private int _ID;
    // public  int ID { get { return _ID; } set { _ID = value; NotifyPropertyChanged(); } }

    [ObservableProperty]
    private string _GenreName;
    // public  string GenreName { get { return _GenreName; } set { _GenreName = value; NotifyPropertyChanged(); } }

}