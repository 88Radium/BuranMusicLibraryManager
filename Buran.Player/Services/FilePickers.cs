using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace Buran.Player.Services;

public static class FilePickers {
    public static async Task<string?> OpenM3uAsync() {
        var top = TopLevel();
        if (top is null)
            return null;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {
            Title          = Buran.Localization.L.Get("Player.ImportM3u"),
            AllowMultiple  = false,
            FileTypeFilter = [M3uType()]
        });
        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public static async Task<string?> SaveM3uAsync(string suggestedName) {
        var top = TopLevel();
        if (top is null)
            return null;

        var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {
            Title                  = Buran.Localization.L.Get("Player.ExportM3u"),
            SuggestedFileName      = suggestedName,
            DefaultExtension       = "m3u",
            FileTypeChoices        = [M3uType()],
            ShowOverwritePrompt    = true
        });
        return file?.Path.LocalPath;
    }

    private static FilePickerFileType M3uType() => new("M3U") {
        Patterns           = ["*.m3u", "*.m3u8"],
        MimeTypes          = ["audio/x-mpegurl", "audio/mpegurl"],
        AppleUniformTypeIdentifiers = ["public.m3u-playlist"]
    };

    private static Avalonia.Controls.TopLevel? TopLevel() {
        var lifetime = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        return lifetime?.MainWindow ?? lifetime?.Windows?.FirstOrDefault();
    }
}
