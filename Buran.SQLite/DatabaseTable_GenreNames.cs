namespace Buran.SQLite;

public class DatabaseTable_GenreNames : INotifyPropertyChanged {

    #region PropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion

    private int _ID;
    public  int ID { get { return _ID; } set { _ID = value; NotifyPropertyChanged(); } }

    private string _GenreName;
    public  string GenreName { get { return _GenreName; } set { _GenreName = value; NotifyPropertyChanged(); } }

}