using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;

namespace Buran.DBEditor.Views;

public partial class DBEditorTab : UserControl {
    public DBEditorTab() {
        AvaloniaXamlLoader.Load(this);
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e) {
        foreach (var list in this.GetVisualDescendants().OfType<ListBox>())
            ListBoxItemContextForward.Attach(list);
    }
}
