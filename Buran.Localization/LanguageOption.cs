using System.ComponentModel;

namespace Buran.Localization;

public sealed class LanguageOption : INotifyPropertyChanged {
    public static IReadOnlyList<LanguageOption> All { get; } = [
        new(LocalizationService.AutoCode, nativeName: ""),
        new("en", "English"),
        new("de", "Deutsch"),
        new("ru", "Русский")
    ];

    public LanguageOption(string code, string nativeName) {
        Code       = code;
        NativeName = nativeName;
        L.Current.LanguageChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
    }

    public string Code       { get; }
    public string NativeName { get; }

    public string Label =>
        Code == LocalizationService.AutoCode ? L.Get("Language.Auto") : NativeName;

    public event PropertyChangedEventHandler? PropertyChanged;

    public static LanguageOption FromCode(string? code) {
        var normalized = string.IsNullOrWhiteSpace(code)
            ? LocalizationService.AutoCode
            : code;
        return All.FirstOrDefault(o => o.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase))
               ?? All[0];
    }
}
