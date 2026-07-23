using System.Collections.Generic;
using Avalonia.Controls;
using Buran.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuranUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase {
    [ObservableProperty] private static ICollection<TabItem>? _tabs;
}