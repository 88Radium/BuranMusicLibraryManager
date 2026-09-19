using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Avalonia.Threading;

namespace Buran.Localization;

/// <summary>
/// Runtime-switchable UI strings. Debug/console output stays English and is not routed through here.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged {
    public const string AutoCode      = "auto";
    public const string FallbackCode  = "en";

    public static readonly string[] SupportedCodes = ["en", "de", "ru", "uz"];

    public static LocalizationService Instance { get; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new() {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly Dictionary<string, string> _english;
    private          Dictionary<string, string> _active;
    private          string                     _preference = AutoCode;
    private          string                     _resolved   = FallbackCode;

    private LocalizationService() {
        _english = LoadDictionary(FallbackCode);
        _active  = new Dictionary<string, string>(_english, StringComparer.Ordinal);
        ApplyPreference(AutoCode, force: true);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler?                LanguageChanged;

    public string Preference => _preference;
    public string ResolvedCode => _resolved;

    public string this[string key] => Get(key);

    public void Initialize(string? preference) => ApplyPreference(preference, force: true);

    public void SetLanguage(string? preference) => ApplyPreference(preference, force: false);

    public string Get(string key) {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        if (_active.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
            return value;

        if (_english.TryGetValue(key, out var fallback) && !string.IsNullOrEmpty(fallback))
            return fallback;

        return key;
    }

    public string Format(string key, params object?[] args) {
        var format = Get(key);
        if (args.Length == 0)
            return format;

        try {
            return string.Format(CultureInfo.CurrentUICulture, format, args);
        }
        catch (FormatException) {
            return format;
        }
    }

    public static string DetectSystemLanguage() {
        return MapToSupported(CultureInfo.CurrentUICulture);
    }

    private void ApplyPreference(string? preference, bool force) {
        var normalized = NormalizePreference(preference);
        if (!force && string.Equals(_preference, normalized, StringComparison.OrdinalIgnoreCase))
            return;

        _preference = normalized;
        _resolved   = normalized == AutoCode ? DetectSystemLanguage() : normalized;

        var next = new Dictionary<string, string>(_english, StringComparer.Ordinal);
        if (!string.Equals(_resolved, FallbackCode, StringComparison.OrdinalIgnoreCase)) {
            foreach (var pair in LoadDictionary(_resolved))
                next[pair.Key] = pair.Value;
        }

        _active = next;
        ApplyCulture(_resolved);
        RaiseChanged();
    }

    private static string NormalizePreference(string? preference) {
        if (string.IsNullOrWhiteSpace(preference) ||
            preference.Equals(AutoCode, StringComparison.OrdinalIgnoreCase))
            return AutoCode;

        var mapped = MapToSupported(preference);
        return SupportedCodes.Contains(mapped, StringComparer.OrdinalIgnoreCase) ? mapped : FallbackCode;
    }

    private static string MapToSupported(string cultureName) {
        try {
            return MapToSupported(CultureInfo.GetCultureInfo(cultureName));
        }
        catch (CultureNotFoundException) {
            return FallbackCode;
        }
    }

    private static string MapToSupported(CultureInfo culture) {
        for (var current = culture;
             current != null && !Equals(current, CultureInfo.InvariantCulture);
             current = current.Parent) {
            var twoLetter = current.TwoLetterISOLanguageName;
            if (SupportedCodes.Contains(twoLetter, StringComparer.OrdinalIgnoreCase))
                return twoLetter;

            if (current.Parent == current || string.IsNullOrEmpty(current.Name))
                break;
        }

        return FallbackCode;
    }

    private static void ApplyCulture(string languageCode) {
        CultureInfo culture;
        try {
            // Prefer Latin Uzbek so system "uz" / "uz-Cyrl" still format like the Latin UI.
            var cultureName = languageCode.Equals("uz", StringComparison.OrdinalIgnoreCase)
                ? "uz-Latn"
                : languageCode;
            culture = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException) {
            culture = CultureInfo.GetCultureInfo(FallbackCode);
        }

        CultureInfo.CurrentUICulture             = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    private void RaiseChanged() {
        void raise() {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item"));
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        try {
            if (Dispatcher.UIThread.CheckAccess())
                raise();
            else
                Dispatcher.UIThread.Post(raise);
        }
        catch {
            raise();
        }
    }

    private static Dictionary<string, string> LoadDictionary(string culture) {
        var assembly = typeof(LocalizationService).Assembly;
        var name     = $"Buran.Localization.Strings.{culture}.json";
        var stream   = assembly.GetManifestResourceStream(name);

        if (stream is null) {
            var suffix = $".Strings.{culture}.json";
            var match  = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
                stream = assembly.GetManifestResourceStream(match);
        }

        if (stream is null)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        using (stream) {
            using var reader = new StreamReader(stream);
            var json   = reader.ReadToEnd();
            var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            return parsed is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(parsed, StringComparer.Ordinal);
        }
    }
}

public static class L {
    public static LocalizationService Current => LocalizationService.Instance;

    public static string Get(string key) => Current.Get(key);

    public static string Format(string key, params object?[] args) => Current.Format(key, args);

    public static void Initialize(string? preference) => Current.Initialize(preference);

    public static void SetLanguage(string? preference) => Current.SetLanguage(preference);

    public static void WhenChanged(Action handler) =>
        Current.LanguageChanged += (_, _) => handler();
}
