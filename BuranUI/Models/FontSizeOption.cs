using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Buran.Localization;
using BuranUI.Services;

namespace BuranUI.Models;

public sealed class FontSizeOption : INotifyPropertyChanged {
    public static IReadOnlyList<FontSizeOption> All { get; } = [
        new(UiFontScale.Small),
        new(UiFontScale.Medium),
        new(UiFontScale.Large)
    ];

    public FontSizeOption(string code) {
        Code = code;
        L.Current.LanguageChanged += (_, _) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
    }

    public string Code { get; }

    public string Label => L.Get($"Settings.Font.{Code}");

    public event PropertyChangedEventHandler? PropertyChanged;

    public static FontSizeOption FromCode(string? code) {
        var normalized = UiFontScale.Normalize(code);
        return All.First(o => o.Code == normalized);
    }
}
