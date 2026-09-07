using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace BuranUI.Services;

public static class FolderPickerService {
    public static async Task<string?> PickFolderAsync(string? title = null) {
        title ??= Buran.Localization.L.Get("FolderPicker.DefaultTitle");
        try {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            TopLevel? tl = lifetime?.MainWindow ?? lifetime?.Windows?.FirstOrDefault();
            if (tl is null)
                return await TryExternalLinuxPicker();

            try {
                var folders = await tl.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                    Title          = title,
                    AllowMultiple  = false,
                });
                if (folders is { Count: > 0 })
                    return folders[0].Path.AbsolutePath;
            }
            catch (Exception ex) {
                Debug.WriteLine($"StorageProvider failed: {ex.Message}");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return await TryExternalLinuxPicker();
        }
        catch (Exception ex) {
            Debug.WriteLine($"PickFolderAsync: {ex}");
        }

        return null;
    }

    private static async Task<string?> TryExternalLinuxPicker() {
        foreach (var (bin, args) in new[] {
                     ("/usr/bin/zenity", "--file-selection --directory"),
                     ("/usr/bin/kdialog", "--getexistingdirectory"),
                 }) {
            try {
                if (!File.Exists(bin))
                    continue;

                var psi = new ProcessStartInfo(bin, args) {
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                };
                using var process = Process.Start(psi);
                if (process is null)
                    continue;

                var output = (await process.StandardOutput.ReadToEndAsync()).Trim();
                await process.WaitForExitAsync();
                if (!string.IsNullOrEmpty(output))
                    return output;
            }
            catch (Exception ex) {
                Debug.WriteLine($"{bin} failed: {ex.Message}");
            }
        }

        return null;
    }
}
