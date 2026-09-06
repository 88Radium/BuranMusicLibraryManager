using Avalonia.Controls;

namespace BuranUI.Views;

public partial class SplashWindow : Window {
    public SplashWindow() {
        InitializeComponent();
    }

    public void SetStatus(string text) {
        StatusText.Text = text;
    }
}
