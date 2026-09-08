using System.ComponentModel;

namespace Buran.ID3Editor.Models;

public sealed class BulkTagItem : INotifyPropertyChanged {
    private bool _applyToAll;

    public BulkTagItem(string name, bool applyToAll = false) {
        Name        = name;
        _applyToAll = applyToAll;
    }

    public string Name { get; }

    public bool ApplyToAll {
        get => _applyToAll;
        set {
            if (_applyToAll == value)
                return;
            _applyToAll = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ApplyToAll)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public override string ToString() => Name;
}
