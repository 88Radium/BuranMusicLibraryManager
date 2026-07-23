namespace Buran.SQLite;

public class DatabaseTable_AlternativeArtistNameVariants : INotifyPropertyChanged {
    #region PropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    public DatabaseTable_AlternativeArtistNameVariants(DataRow pEntry) {
        if (pEntry != null) {
            ID                    = Convert.ToInt32(pEntry.ItemArray[0]);
            AlternativeArtistName = Convert.ToString(pEntry.ItemArray[1]);
            RefersToArtistName    = Convert.ToInt32(pEntry.ItemArray[3]);
            IsMissSpelled         = Convert.ToInt32(pEntry.ItemArray[2]);
        }
    }

    private int _ID;

    public int ID {
        get { return _ID; }
        set {
            _ID = value;
            NotifyPropertyChanged();
        }
    }

    private string _AlternativeArtistName;

    public string AlternativeArtistName {
        get { return _AlternativeArtistName; }
        set {
            _AlternativeArtistName = value;
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

    private int _RefersToArtistName;

    public int RefersToArtistName {
        get { return _RefersToArtistName; }
        set {
            _RefersToArtistName = value;
            NotifyPropertyChanged();
        }
    }
}