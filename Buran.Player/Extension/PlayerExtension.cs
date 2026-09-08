using System.Composition;
using Avalonia.Controls;
using Buran.Interfaces;
using Buran.Localization;
using Buran.Player.ViewModels;
using Buran.Player.Views;

namespace Buran.Player.Extension;

[Export(typeof(IExtension))]
public class PlayerExtension : IExtension, IMusicFolderConsumer, IPlaybackController, IPlaylistSink, IPlayerDock {
    public           TabItem     Tab { get; set; }
    private readonly PlayerTab          _view;
    private readonly PlayerChrome       _chrome;
    private readonly PlayerDockView     _dock;
    private readonly PlayerTabViewModel _vm;

    public PlayerExtension() {
        _vm     = new PlayerTabViewModel();
        _chrome = new PlayerChrome { DataContext = _vm };
        _dock   = new PlayerDockView { DataContext = _vm };
        _view   = new PlayerTab { DataContext = _vm };
        _view.PlaceChrome(_chrome);
        Tab = new TabItem {
            Header  = L.Get("Tab.Player"),
            Content = _view
        };
        L.WhenChanged(() => Tab.Header = L.Get("Tab.Player"));
    }

    private PlayerTabViewModel Vm => _vm;

    public Control TakeDock() {
        _vm.IsDocked = true;
        _view.ReleaseChrome();
        _dock.PlaceChrome(_chrome);
        return _dock;
    }

    public void LoadMusicFolder(string folderPath, bool includeSubfolders) =>
        Vm.LoadMusicFolder(folderPath, includeSubfolders);

    public void PlayFile(string path, IReadOnlyList<string>? queue = null) =>
        Vm?.PlayFile(path, queue);

    public void StopPlayback() =>
        Vm?.StopPlayback();

    public void AddTracks(IReadOnlyList<string> paths) =>
        Vm?.AddTracks(paths);
}
