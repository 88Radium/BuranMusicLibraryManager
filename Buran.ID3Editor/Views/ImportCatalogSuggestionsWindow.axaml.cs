using Avalonia.Controls;
using Avalonia.Input;
using Buran.ID3Editor.ViewModels;

namespace Buran.ID3Editor.Views;

public partial class ImportCatalogSuggestionsWindow : Window {
    public ImportCatalogSuggestionsWindow() {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e) {
        if (e.Key == Key.Escape) {
            if (DataContext is ImportCatalogSuggestionsViewModel vm)
                vm.SkipCommand.Execute(null);
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }
}
