using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Buran.Player.ViewModels;

namespace Buran.Player.Views;

public partial class PlayerChrome : UserControl {
    public PlayerChrome() {
        AvaloniaXamlLoader.Load(this);
    }

    private void Spectrogram_SeekRequested(object? sender, double position) {
        if (DataContext is PlayerTabViewModel vm)
            vm.SeekTo(position);
    }
}
