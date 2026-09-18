using Avalonia.Controls;
using Buran.Localization;
using BuranUI.Services;

namespace BuranUI.Views;

public partial class SplashWindow : Window {
    public SplashWindow() {
        InitializeComponent();
        VersionText.Text = L.Format("Settings.Version", AppVersion.Display);
    }

    public void SetStatus(string text) {
        StatusText.Text = text;
    }
}
