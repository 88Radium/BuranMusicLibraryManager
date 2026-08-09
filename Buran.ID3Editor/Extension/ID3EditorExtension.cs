using System.Composition;
using Avalonia.Controls;
using Buran.ID3Editor.Views;
using Buran.Interfaces;

namespace Buran.ID3Editor.Extension;

[Export(typeof(IExtension))]
public class Id3EditorExtension : IExtension {
    public TabItem Tab { get; set; }
    private readonly Id3EditorTab _view;

    public Id3EditorExtension() {
        // ✅ Speichere die View als Feld, damit sie nicht vom GC gesammelt wird
        _view = new Id3EditorTab();
        Tab = _view.Id3EditorTabControl;
    }
}