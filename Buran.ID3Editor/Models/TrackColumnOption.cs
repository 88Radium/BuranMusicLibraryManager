using Buran.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.ID3Editor.Models;

public partial class TrackColumnOption : ObservableObject {
    public required string Id { get; init; }
    public required string HeaderKey { get; init; }
    public required string Binding { get; init; }

    public string Header => L.Get(HeaderKey);

    [ObservableProperty] private bool _isVisible = true;
}
