using System;
using Avalonia.Controls;
using BuranUI.ViewModels;

namespace BuranUI.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();

        if (MainWindowViewModel.Tabs is not null) {
            foreach (var tab in MainWindowViewModel.Tabs)
                MainTabControl.Items.Add(tab);
        }

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e) {
        if (DataContext is MainWindowViewModel vm)
            vm.Library.RequestId3Tab += (_, _) => SelectId3Tab();
    }

    private void SelectId3Tab() {
        foreach (var item in MainTabControl.Items) {
            if (item is TabItem { Header: string header } tab &&
                header.Contains("ID3", StringComparison.OrdinalIgnoreCase)) {
                MainTabControl.SelectedItem = tab;
                break;
            }
        }
    }
}