using System.Composition;
using Avalonia.Controls;
using Buran.DBEditor.Views;
using Buran.Interfaces;

namespace Buran.DBEditor.Extension;

[Export(typeof(IExtension))]
public class DBEditorExtension : IExtension {
    public           TabItem     Tab { get; set; }
    private readonly DBEditorTab _view;

    public DBEditorExtension() {
        // ✅ Speichere die View als Feld, damit sie nicht vom GC gesammelt wird
        _view = new DBEditorTab();
        Tab = new TabItem {
            Header  = "DB Editor",
            Content = _view
        };
    }
}