using Avalonia.Controls;
using Avalonia.Input;
using Buran.ID3Editor.ViewModels;
using Buran.Types;

namespace Buran.ID3Editor.Views;

public partial class Id3EditorTab : UserControl {
    public Id3EditorTab() {
        InitializeComponent();

    }

    private void CatalogAddBox_KeyDown(object? sender, KeyEventArgs e) {
        if (e.Key != Key.Enter || sender is not AutoCompleteBox { DataContext: Mp3FileObject file } box)
            return;
        if (DataContext is not Id3EditorTabViewModel vm)
            return;
        if (box.IsDropDownOpen)
            return;

        switch (box.Tag as string) {
            case "Artist":
                vm.AddSingleArtistFromID3Tags(file);
                break;
            case "Genre":
                vm.AddSingleGenreFromID3Tags(file);
                break;
            case "Mood":
                vm.AddSingleMoodFromID3Tags(file);
                break;
        }

        e.Handled = true;
    }
}