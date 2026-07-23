namespace Buran.SQLite;

public class DatabaseTable_AlternativeGenreNameVariants : INotifyPropertyChanged {
    #region PropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    private int _ID;

    public int ID {
        get { return _ID; }
        set {
            _ID = value;
            NotifyPropertyChanged();
        }
    }

    private string _GenreNameVariant;

    public string GenreNameVariant {
        get { return _GenreNameVariant; }
        set {
            _GenreNameVariant = value;
            NotifyPropertyChanged();
        }
    }

    private int _IsMissSpelled;

    public int IsMissSpelled {
        get { return _IsMissSpelled; }
        set {
            _IsMissSpelled = value;
            NotifyPropertyChanged();
        }
    }

    private int _RefersToGenreName;

    public int RefersToGenreName {
        get { return _RefersToGenreName; }
        set {
            _RefersToGenreName = value;
            NotifyPropertyChanged();
        }
    }
}