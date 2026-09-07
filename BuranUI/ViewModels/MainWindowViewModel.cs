using System.Collections.Generic;
using Avalonia.Controls;
using Buran.Interfaces;
using Buran.Localization;
using BuranUI.Models;
using BuranUI.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuranUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    public static ICollection<TabItem>? Tabs { get; set; }
    public static IList<IExtension>?    LoadedExtensions { get; set; }

    private readonly UiSettings _settings;

    public MainWindowViewModel() {
        _settings           = UiSettings.Load();
        _windowOpacity      = _settings.WindowOpacity;
        _selectedLanguage   = LanguageOption.FromCode(_settings.Language);
        _selectedFontSize   = FontSizeOption.FromCode(_settings.FontSize);
        Library             = new LibraryBrowserViewModel(_settings);
        UiFontScale.Apply(_selectedFontSize.Code);
    }

    public LibraryBrowserViewModel Library { get; }

    public IReadOnlyList<LanguageOption> LanguageOptions => LanguageOption.All;
    public IReadOnlyList<FontSizeOption> FontSizeOptions => FontSizeOption.All;

    [ObservableProperty] private double         _windowOpacity    = 0.92;
    [ObservableProperty] private LanguageOption _selectedLanguage = LanguageOption.All[0];
    [ObservableProperty] private FontSizeOption _selectedFontSize = FontSizeOption.All[1];

    partial void OnWindowOpacityChanged(double value) {
        _settings.WindowOpacity = value;
        _settings.Save();
    }

    partial void OnSelectedLanguageChanged(LanguageOption value) {
        L.SetLanguage(value.Code);
        _settings.Language = value.Code;
        _settings.Save();
    }

    partial void OnSelectedFontSizeChanged(FontSizeOption value) {
        _settings.FontSize = value.Code;
        _settings.Save();
        UiFontScale.Apply(value.Code);
    }
}