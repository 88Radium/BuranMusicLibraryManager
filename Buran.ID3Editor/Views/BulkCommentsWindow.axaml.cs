using Avalonia.Controls;
using Avalonia.Input;
using Buran.ID3Editor.ViewModels;

namespace Buran.ID3Editor.Views;

public partial class BulkCommentsWindow : Window {
    public BulkCommentsWindow() {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            if (DataContext is BulkCommentsViewModel vm)
                vm.CancelCommand.Execute(null);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
