using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace BuranUI.Views;

public partial class HelpWindow : Window {
    public HelpWindow() {
        InitializeComponent();
    }

    private void Close_Click(object? sender, RoutedEventArgs e) => Close();

    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            Close();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
