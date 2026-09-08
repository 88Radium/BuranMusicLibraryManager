using Avalonia.Controls;
using Avalonia.Input;
using Buran.ID3Editor.ViewModels;

namespace Buran.ID3Editor.Views;

public partial class CompareFilesWindow : Window {
    public CompareFilesWindow() {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            if (DataContext is CompareFilesViewModel vm)
                vm.ChooseKeepBothCommand.Execute(null);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
