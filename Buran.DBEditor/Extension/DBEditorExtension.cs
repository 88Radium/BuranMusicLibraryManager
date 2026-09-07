using System.Composition;
using Avalonia.Controls;
using Buran.DBEditor.Views;
using Buran.Interfaces;
using Buran.Localization;

namespace Buran.DBEditor.Extension;

[Export(typeof(IExtension))]
public class DBEditorExtension : IExtension {
    public           TabItem     Tab { get; set; }
    private readonly DBEditorTab _view;

    public DBEditorExtension() {
        _view = new DBEditorTab();
        Tab = new TabItem {
            Header  = L.Get("Tab.DbEditor"),
            Content = _view
        };
        L.WhenChanged(() => Tab.Header = L.Get("Tab.DbEditor"));
    }
}