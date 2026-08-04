using System.Composition;
using Avalonia.Controls;
using Buran.ID3Editor.Views;
using Buran.Interfaces;

namespace Buran.ID3Editor.Extension;

[Export(typeof(IExtension))]
public class Id3EditorExtension : IExtension {
    public TabItem Tab { get; set; }

    public Id3EditorExtension() {
        Tab = new Id3EditorTab().ID3EditorTabControl;
    }
}