using Avalonia.Controls;
using Avalonia.Input;
using Buran.ID3Editor.ViewModels;

namespace Buran.ID3Editor.Views;

public partial class BulkAddTagsWindow : Window {
    public BulkAddTagsWindow() {
        InitializeComponent();
        Opened += (_, _) => TagInputBox.Focus();
    }

    private void TagInputBox_KeyDown(object? sender, KeyEventArgs e) {
        if (e.Key != Key.Enter)
            return;
        if (sender is AutoCompleteBox { IsDropDownOpen: true })
            return;
        if (DataContext is not BulkAddTagsViewModel vm)
            return;

        vm.AddTagCommand.Execute(null);
        e.Handled = true;
        TagInputBox.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            if (DataContext is BulkAddTagsViewModel vm)
                vm.CancelCommand.Execute(null);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
