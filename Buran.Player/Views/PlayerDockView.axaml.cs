using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Buran.Player.Models;
using Buran.Player.ViewModels;

namespace Buran.Player.Views;

public partial class PlayerDockView : UserControl {
    private const double MinPlaylistWidth   = 160;
    private const double MinVisualizerWidth = 160;
    private const double ToggleButtonWidth  = 36;

    private bool   _splitDragging;
    private double _splitStartWidth;
    private double _splitStartX;

    public PlayerDockView() {
        AvaloniaXamlLoader.Load(this);
    }

    public void PlaceChrome(Control chrome) {
        var host = this.FindControl<ContentControl>("ChromeHost");
        if (host is not null)
            host.Content = chrome;
    }

    private void PlaylistTracks_DoubleTapped(object? sender, TappedEventArgs e) {
        if (DataContext is PlayerTabViewModel vm && vm.SelectedPlaylistTrack is PlaylistTrack track)
            vm.PlayPlaylistTrackCommand.Execute(track);
    }

    private void PlaylistSplitter_PointerPressed(object? sender, PointerPressedEventArgs e) {
        if (DataContext is not PlayerTabViewModel vm)
            return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;
        _splitDragging   = true;
        _splitStartWidth = vm.PlaylistPanelWidth;
        _splitStartX     = e.GetPosition(this).X;
        e.Pointer.Capture(sender as IInputElement);
        e.Handled = true;
    }

    private void PlaylistSplitter_PointerMoved(object? sender, PointerEventArgs e) {
        if (!_splitDragging || DataContext is not PlayerTabViewModel vm)
            return;
        var delta = e.GetPosition(this).X - _splitStartX;
        var max   = Math.Max(MinPlaylistWidth, Bounds.Width - MinVisualizerWidth - ToggleButtonWidth);
        vm.PlaylistPanelWidth = Math.Clamp(_splitStartWidth - delta, MinPlaylistWidth, max);
    }

    private void PlaylistSplitter_PointerReleased(object? sender, PointerReleasedEventArgs e) {
        EndSplitDrag(e.Pointer);
    }

    private void PlaylistSplitter_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e) {
        _splitDragging = false;
    }

    private void EndSplitDrag(IPointer pointer) {
        if (!_splitDragging)
            return;
        _splitDragging = false;
        pointer.Capture(null);
    }
}
