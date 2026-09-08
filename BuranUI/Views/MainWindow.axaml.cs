using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Buran.Interfaces;
using BuranUI.ViewModels;

namespace BuranUI.Views;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();

        if (MainWindowViewModel.Tabs is not null) {
            foreach (var tab in MainWindowViewModel.Tabs)
                MainTabControl.Items.Add(tab);
        }

        if (MainWindowViewModel.PlayerDock is { } dock)
            PlayerBarHost.Children.Add(dock);
        else {
            PlayerBarHost.IsVisible = false;
            PlayerSplitter.IsVisible = false;
            Grid.SetRow(MainTabControl, 0);
            Grid.SetRowSpan(MainTabControl, 3);
        }

        DataContextChanged += OnDataContextChanged;
        ModuleHub.ActivateId3Editor += (_, _) => SelectId3Tab();
        ApplyPlayerPlacement();
    }

    private void OnDataContextChanged(object? sender, EventArgs e) {
        if (DataContext is MainWindowViewModel vm) {
            vm.Library.RequestId3Tab += (_, _) => SelectId3Tab();
            vm.PropertyChanged += OnVmPropertyChanged;
            ApplyPlayerPlacement();
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(MainWindowViewModel.PlayerOnTop))
            ApplyPlayerPlacement();
    }

    private void ApplyPlayerPlacement() {
        if (MainWindowViewModel.PlayerDock is null)
            return;
        var onTop = DataContext is MainWindowViewModel { PlayerOnTop: true };
        Grid.SetRow(MainTabControl, onTop ? 2 : 0);
        Grid.SetRow(PlayerSplitter, 1);
        Grid.SetRow(PlayerBarHost, onTop ? 0 : 2);
        if (WorkspaceGrid.RowDefinitions.Count >= 3) {
            WorkspaceGrid.RowDefinitions[0].Height = new GridLength(onTop ? 3 : 7, GridUnitType.Star);
            WorkspaceGrid.RowDefinitions[2].Height = new GridLength(onTop ? 7 : 3, GridUnitType.Star);
        }
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