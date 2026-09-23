using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Buran.DBEditor.Views;

/// <summary>
/// TextBox inside a list item eats right-click for Cut/Copy/Paste.
/// Tunnel the request to the ListBoxItem, select it, and open the list menu.
/// </summary>
internal static class ListBoxItemContextForward {
    private static readonly AttachedProperty<bool> IsAttachedProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>("IsAttached", typeof(ListBoxItemContextForward));

    public static void Attach(ListBox listBox) {
        if (listBox.GetValue(IsAttachedProperty))
            return;
        listBox.SetValue(IsAttachedProperty, true);
        listBox.AddHandler(Control.ContextRequestedEvent, OnContextRequested, RoutingStrategies.Tunnel);
    }

    private static void OnContextRequested(object? sender, ContextRequestedEventArgs e) {
        if (sender is not ListBox listBox || listBox.ContextMenu is null)
            return;
        if (e.Source is not Visual source)
            return;

        var item = source as ListBoxItem ?? source.FindAncestorOfType<ListBoxItem>();
        if (item is null)
            return;
        if (!ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(item), listBox))
            return;

        if (item.DataContext is not null)
            listBox.SelectedItem = item.DataContext;

        e.Handled = true;
        if (listBox.ContextMenu.IsOpen)
            listBox.ContextMenu.Close();
        listBox.ContextMenu.Open(item);
    }
}
