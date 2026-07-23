using System.Collections.Generic;
using Avalonia.Controls;

namespace BuranUI.ViewModels;

public class MainWindowViewModel : ViewModelBase {
    public static ICollection<TabItem>? Tabs { get; set; }
}