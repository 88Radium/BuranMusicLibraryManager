using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Buran.Player.Models;
using Buran.Player.ViewModels;

namespace Buran.Player.Views;

public partial class PlayerTab : UserControl {
    public PlayerTab() {
        AvaloniaXamlLoader.Load(this);
    }

    public void PlaceChrome(Control chrome) {
        var host = this.FindControl<ContentControl>("ChromeHost");
        if (host is not null)
            host.Content = chrome;
    }

    public void ReleaseChrome() {
        var host = this.FindControl<ContentControl>("ChromeHost");
        if (host is not null)
            host.Content = null;
    }

    private void FolderTracks_DoubleTapped(object? sender, TappedEventArgs e) {
        if (DataContext is PlayerTabViewModel vm && vm.SelectedFolderTrack is PlaylistTrack track)
            vm.PlayFolderTrackCommand.Execute(track);
    }

    private void PlaylistTracks_DoubleTapped(object? sender, TappedEventArgs e) {
        if (DataContext is PlayerTabViewModel vm && vm.SelectedPlaylistTrack is PlaylistTrack track)
            vm.PlayPlaylistTrackCommand.Execute(track);
    }
}
