using System.Composition;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Buran.ID3Editor.ViewModels;
using Buran.Interfaces;
using FxResources.System.Composition;

namespace Buran.ID3Editor.Views;

public partial class Id3EditorTab : UserControl {
    public Id3EditorTab() {
        InitializeComponent();

        // Warte, bis das Control im Visual Tree ist
        this.AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) {
        // Nur einmal ausführen
        this.AttachedToVisualTree -= OnAttachedToVisualTree;

        var topLevel        = TopLevel.GetTopLevel(this);
        var storageProvider = topLevel?.StorageProvider;

        DataContext = new Id3EditorTabViewModel(storageProvider);
    }

    private void DirPath_TextChanged(object sender, TextChangedEventArgs e) {
        if (DataContext is Id3EditorTabViewModel vm)
            vm.LoadMusicFiles();
    }
}