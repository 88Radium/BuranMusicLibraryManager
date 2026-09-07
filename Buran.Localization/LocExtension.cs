using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace Buran.Localization;

/// <summary>
/// Resolves a translation key. Does not use indexer binding paths, because keys contain dots
/// (e.g. Db.GroupExample) that the binding parser would split.
/// </summary>
public sealed class LocExtension : MarkupExtension {
    public LocExtension() { }

    public LocExtension(string key) {
        Key = key;
    }

    public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider) {
        return new Binding(nameof(LocalizedString.Value)) {
            Source = new LocalizedString(Key),
            Mode   = BindingMode.OneWay
        };
    }
}

public sealed class LocalizedString : INotifyPropertyChanged {
    public LocalizedString(string key) {
        Key = key;
        L.Current.LanguageChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    public string Key { get; }

    public string Value => L.Get(Key);

    public event PropertyChangedEventHandler? PropertyChanged;
}
