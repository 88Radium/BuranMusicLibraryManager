using System.ComponentModel;
using Buran.Localization;
using Buran.Types;

namespace Buran.DBEditor.Models;

public sealed class KeywordTypeOption : INotifyPropertyChanged {
    public KeywordTypeOption(string code) {
        Code = code;
        L.Current.LanguageChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
    }

    public string Code { get; }

    public string Label => CollaborationMarkers.IsVersion(Code)
        ? L.Get("Db.KeywordTypeVersion")
        : L.Get("Db.KeywordTypeCollaboration");

    public event PropertyChangedEventHandler? PropertyChanged;
}
