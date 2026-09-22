using System.Collections.Generic;
using System.Composition;
using Avalonia.Controls;
using Buran.ID3Editor.ViewModels;
using Buran.ID3Editor.Views;
using Buran.Interfaces;
using Buran.Localization;

namespace Buran.ID3Editor.Extension;

[Export(typeof(IExtension))]
public class Id3EditorExtension : IExtension, IMusicFolderConsumer, ITrackListHost {
    public           TabItem      Tab { get; set; }
    private readonly Id3EditorTab _view;

    public Id3EditorExtension() {
        _view = new Id3EditorTab();
        Tab = new TabItem {
            Header  = L.Get("Tab.Id3Editor"),
            Content = _view
        };
        L.WhenChanged(() => Tab.Header = L.Get("Tab.Id3Editor"));
    }

    public void LoadMusicFolder(string folderPath, bool includeSubfolders) {
        if (_view.DataContext is Id3EditorTabViewModel vm)
            vm.LoadFromFolder(folderPath, includeSubfolders);
    }

    public void ShowTracks(IReadOnlyList<string> paths, string caption, string? notice = null) {
        if (_view.DataContext is Id3EditorTabViewModel vm)
            vm.LoadFromPaths(paths, caption, notice);
    }

    public void IndexLibrary(IReadOnlyList<string> rootPaths) {
        if (_view.DataContext is Id3EditorTabViewModel vm)
            vm.StartLibraryIndex(rootPaths);
    }
}