using CommunityToolkit.Mvvm.ComponentModel;

namespace Buran.Player.Models;

public partial class Playlist : ObservableObject {
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [ObservableProperty] private string _name = "";

    public List<string> Paths { get; set; } = [];
}
