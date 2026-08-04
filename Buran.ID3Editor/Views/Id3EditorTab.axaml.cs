using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Buran.ID3Editor.ViewModels;

namespace Buran.ID3Editor.Views;

public partial class Id3EditorTab : UserControl {
    public Id3EditorTab() {
        InitializeComponent();

        // StorageProvider holen und an ViewModel übergeben
        var topLevel        = TopLevel.GetTopLevel(this);
        var storageProvider = topLevel?.StorageProvider;
        DataContext = new Id3EditorTabViewModel(storageProvider);
    }

    private void DirPath_TextChanged(object sender, TextChangedEventArgs e) {
        if (DataContext is Id3EditorTabViewModel vmContext) {
            vmContext.LoadMusicFiles();
        }
    }
    
}