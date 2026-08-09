using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Buran.ID3Editor.ViewModels;

namespace Buran.ID3Editor.Views;

public partial class Id3EditorTab : UserControl {
    public Id3EditorTab() {
        InitializeComponent();

        // Set PickFolderAsync in Loaded event so DataContext and TopLevel are available
        this.Loaded += (_, _) => {
            if (this.DataContext is Id3EditorTabViewModel vm) {
                // Use the ViewModel's BrowseFolder implementation (single source of truth)
                vm.PickFolderAsync = vm.BrowseFolder;
                System.Diagnostics.Debug.WriteLine("✅ PickFolderAsync initialized in Loaded event (vm.BrowseFolder)");
            } else {
                System.Diagnostics.Debug.WriteLine("❌ DataContext is not Id3EditorTabViewModel in Loaded event");
            }
        };
    }

    private void DirPath_TextChanged(object sender, TextChangedEventArgs e) {
        if (DataContext is Id3EditorTabViewModel vm)
            vm.LoadMusicFiles();
    }
}