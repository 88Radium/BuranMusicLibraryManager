using System.Collections.ObjectModel;
using Buran.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuranUI.ViewModels;

public partial class HelpWindowViewModel : ObservableObject {
    public HelpWindowViewModel() {
        Reload();
        L.WhenChanged(Reload);
    }

    public ObservableCollection<HelpBlock> Blocks { get; } = [];

    public string Title => L.Get("Help.Title");

    private void Reload() {
        Blocks.Clear();
        foreach (var block in HelpDocument.Load())
            Blocks.Add(block);
        OnPropertyChanged(nameof(Title));
    }
}
