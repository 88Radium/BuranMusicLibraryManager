using Avalonia.Controls;
using BuranUI.ViewModels;

namespace BuranUI.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        // Avalonia zeigt TabItems aus ItemsSource nicht als Tab-Leiste (anders als WPF).
        // Die von MEF geladenen TabItems müssen direkt in Items.
        if (MainWindowViewModel.Tabs is null) return;
        foreach (var tab in MainWindowViewModel.Tabs)
            MainTabControl.Items.Add(tab);
    }
}