using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Buran.Interfaces;
using Buran.Localization;
using Buran.Types;
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
        L.WhenChanged(() => OnPropertyChanged(nameof(VersionText)));
    }

    public LibraryBrowserViewModel Library { get; }

    public string VersionText => L.Format("Settings.Version", AppVersion.Display);

    public IReadOnlyList<LanguageOption> LanguageOptions => LanguageOption.All;
    public IReadOnlyList<FontSizeOption> FontSizeOptions => FontSizeOption.All;

    [ObservableProperty] private double         _windowOpacity    = 0.92;
    [ObservableProperty] private LanguageOption _selectedLanguage = LanguageOption.All[0];
    [ObservableProperty] private FontSizeOption _selectedFontSize = FontSizeOption.All[1];
    [ObservableProperty] private bool           _playerOnTop;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool _isCheckingUpdates;

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

    public async Task NotifyIfUpdateAvailableAsync() {
        if (IsCheckingUpdates)
            return;

        IsCheckingUpdates = true;
        try {
            var result = await RunCheckAsync();
            if (result.Kind != UpdateKind.Available)
                return;
            if (string.Equals(_settings.DismissedUpdateVersion, result.RemoteDisplay, StringComparison.Ordinal))
                return;
            await OfferDownloadAsync(result);
        }
        catch {
            // Offline or GitHub down: the next start tries again.
        }
        finally {
            IsCheckingUpdates = false;
        }
    }

    private bool CanCheckForUpdates() => !IsCheckingUpdates;

    [RelayCommand(CanExecute = nameof(CanCheckForUpdates))]
    private async Task CheckForUpdates() {
        if (IsCheckingUpdates)
            return;

        IsCheckingUpdates = true;
        try {
            var result = await RunCheckAsync();
            switch (result.Kind) {
                case UpdateKind.Available:
                    await OfferDownloadAsync(result);
                    break;
                case UpdateKind.NewerWithoutInstaller:
                    await BuranMessageBox.Show(L.Format(
                        "Update.AvailableNoAsset",
                        result.RemoteDisplay,
                        result.InstalledDisplay,
                        OsLabel(result.Platform)));
                    break;
                case UpdateKind.UpToDate:
                    await BuranMessageBox.Show(L.Format(
                        "Update.Current",
                        OsLabel(result.Platform),
                        result.InstalledDisplay));
                    break;
                default:
                    await BuranMessageBox.Show(L.Get("Update.Failed"));
                    break;
            }
        }
        catch {
            await BuranMessageBox.Show(L.Get("Update.Failed"));
        }
        finally {
            IsCheckingUpdates = false;
        }
    }

    private static async Task<UpdateDecision> RunCheckAsync() {
        var installed = UpdateCheck.ParseVersionOrZero(AppVersion.Display);
        var platform  = HostPlatform.Detect();
        var release   = await GitHubUpdateClient.FetchLatestAsync();
        if (release is null)
            return new UpdateDecision { Kind = UpdateKind.Failed, Installed = installed, Platform = platform };

        return UpdateCheck.Decide(
            installed,
            release.TagName,
            release.Prerelease,
            release.Draft,
            release.Assets,
            platform);
    }

    private async Task OfferDownloadAsync(UpdateDecision result) {
        var open = await BuranMessageBox.AskYesNo(L.Format(
            "Update.Available",
            result.RemoteDisplay,
            OsLabel(result.Platform),
            result.InstalledDisplay,
            result.Asset?.Name));
        _settings.DismissedUpdateVersion = result.RemoteDisplay;
        _settings.Save();
        if (open && result.Asset is not null)
            await OpenUrl(result.Asset.Url);
    }

    private static string OsLabel(HostPlatform platform) => platform.Os switch {
        HostOsKind.Windows => L.Get("Update.Os.Windows"),
        HostOsKind.Linux   => L.Get("Update.Os.Linux"),
        HostOsKind.MacOs   => L.Get("Update.Os.Mac"),
        _                  => L.Get("Update.Os.Other")
    };

    private static async Task OpenUrl(string url) {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var top = lifetime?.MainWindow is { } window ? TopLevel.GetTopLevel(window) : null;
        if (top?.Launcher is not null && await top.Launcher.LaunchUriAsync(uri))
            return;

        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }
}