using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Buran.Interfaces;
using Buran.Localization;
using BuranUI.Models;
using BuranUI.Services;
using BuranUI.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BuranUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    public static ICollection<TabItem>? Tabs { get; set; }
    public static IList<IExtension>?    LoadedExtensions { get; set; }
    public static Control?              PlayerDock { get; set; }

    private readonly UiSettings _settings;

    public MainWindowViewModel() {
        _settings           = UiSettings.Load();
        _windowOpacity      = _settings.WindowOpacity;
        _selectedLanguage   = LanguageOption.FromCode(_settings.Language);
        _selectedFontSize   = FontSizeOption.FromCode(_settings.FontSize);
        _playerOnTop        = _settings.PlayerOnTop;
        Library             = new LibraryBrowserViewModel(_settings);
        UiFontScale.Apply(_selectedFontSize.Code);
    }

    public LibraryBrowserViewModel Library { get; }

    public IReadOnlyList<LanguageOption> LanguageOptions => LanguageOption.All;
    public IReadOnlyList<FontSizeOption> FontSizeOptions => FontSizeOption.All;

    [ObservableProperty] private double         _windowOpacity    = 0.92;
    [ObservableProperty] private LanguageOption _selectedLanguage = LanguageOption.All[0];
    [ObservableProperty] private FontSizeOption _selectedFontSize = FontSizeOption.All[1];
    [ObservableProperty] private bool           _playerOnTop;

    public bool HasPlayerDock => PlayerDock is not null;

    public int TabRow    => PlayerOnTop && HasPlayerDock ? 2 : 0;
    public int PlayerRow => PlayerOnTop && HasPlayerDock ? 0 : 2;

    public GridLength PlayerRowHeight =>
        HasPlayerDock ? new GridLength(3, GridUnitType.Star) : new GridLength(0);

    public GridLength TabRowHeight =>
        HasPlayerDock ? new GridLength(7, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);

    public GridLength PlayerSplitterHeight =>
        HasPlayerDock ? new GridLength(6) : new GridLength(0);

    partial void OnPlayerOnTopChanged(bool value) {
        _settings.PlayerOnTop = value;
        _settings.Save();
        OnPropertyChanged(nameof(TabRow));
        OnPropertyChanged(nameof(PlayerRow));
    }

    [RelayCommand]
    private void SwapPlayerAndTabs() {
        if (HasPlayerDock)
            PlayerOnTop = !PlayerOnTop;
    }

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

    [RelayCommand]
    private async Task OpenHelp() {
        var dialog = new HelpWindow {
            DataContext = new HelpWindowViewModel()
        };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } owner })
            await dialog.ShowDialog(owner);
        else
            dialog.Show();
    }
}