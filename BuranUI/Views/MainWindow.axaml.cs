using Avalonia.Controls;
using BuranUI.ViewModels;

namespace BuranUI.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel();
    }
}