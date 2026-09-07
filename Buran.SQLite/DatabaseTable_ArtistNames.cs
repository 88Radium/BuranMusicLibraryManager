using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;

namespace Buran.SQLite;

public class DatabaseTable_ArtistNames : INotifyPropertyChanged {
    #region PropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    public DatabaseTable_ArtistNames() { }

    public DatabaseTable_ArtistNames(DataRow pEntry) {
        if (pEntry != null) {
            ID                  = Convert.ToInt32(pEntry.ItemArray[0]);
            PreferredArtistName = Convert.ToString(pEntry.ItemArray[1]) ?? "";
            RealName            = Convert.ToString(pEntry.ItemArray[2]) ?? "";
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


    private string _PreferredArtistName = "";

    public string PreferredArtistName {
        get { return _PreferredArtistName; }
        set {
            _PreferredArtistName = value;
            NotifyPropertyChanged();
        }
    }


    private string _RealName = "";

    public string RealName {
        get { return _RealName; }
        set {
            _RealName = value;
            NotifyPropertyChanged();
        }
    }
}