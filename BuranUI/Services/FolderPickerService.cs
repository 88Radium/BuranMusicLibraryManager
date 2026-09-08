using System;
using System.Collections.Generic;
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
    public static async Task<string?> PickFolderAsync(string? title = null, string? startPath = null) {
        var folders = await PickFoldersAsync(title, startPath, allowMultiple: false);
        return folders.Count > 0 ? folders[0] : null;
    }

    public static async Task<IReadOnlyList<string>> PickFoldersAsync(
        string? title = null,
        string? startPath = null,
        bool allowMultiple = true) {
        title ??= Buran.Localization.L.Get("FolderPicker.DefaultTitle");
        startPath = ResolveStartPath(startPath);

        // Linux portals + GTK often stack dialogs; zenity/kdialog is one shot.
        // Never chain pickers: cancel means cancel, not "try the next backend".
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return await TryExternalLinuxPicker(title, startPath, allowMultiple);

        try {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            TopLevel? tl = lifetime?.MainWindow ?? lifetime?.Windows?.FirstOrDefault();
            if (tl?.StorageProvider is not { CanPickFolder: true } storage)
                return [];

            IStorageFolder? start = null;
            if (!string.IsNullOrWhiteSpace(startPath) && Directory.Exists(startPath))
                start = await storage.TryGetFolderFromPathAsync(startPath);

            IReadOnlyList<IStorageFolder> folders;
            try {
                folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions {
                    Title                  = title,
                    AllowMultiple          = allowMultiple,
                    SuggestedStartLocation = start
                });
            }
            catch (OperationCanceledException) {
                return [];
            }

            return folders
                .Select(f => f.TryGetLocalPath())
                .Where(p => !string.IsNullOrWhiteSpace(p) && Directory.Exists(p))
                .Select(p => Path.GetFullPath(p!))
                .ToList();
        }
        catch (Exception ex) {
            Debug.WriteLine($"PickFolderAsync: {ex}");
            return [];
        }
    }

    private static string? ResolveStartPath(string? startPath) {
        if (!string.IsNullOrWhiteSpace(startPath) && Directory.Exists(startPath))
            return Path.GetFullPath(startPath);

        var music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        if (!string.IsNullOrWhiteSpace(music) && Directory.Exists(music))
            return music;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home) && Directory.Exists(home))
            return home;

        return null;
    }

    private static async Task<IReadOnlyList<string>> TryExternalLinuxPicker(
        string title,
        string? startPath,
        bool allowMultiple) {
        startPath = ResolveStartPath(startPath);
        var desktop = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP") ?? "";
        var kde     = desktop.Contains("KDE", StringComparison.OrdinalIgnoreCase);

        if (allowMultiple && File.Exists("/usr/bin/zenity"))
            return await RunZenity(title, startPath, allowMultiple: true);
        if (kde && File.Exists("/usr/bin/kdialog"))
            return await RunKdialog(title, startPath);
        if (File.Exists("/usr/bin/zenity"))
            return await RunZenity(title, startPath, allowMultiple);
        if (File.Exists("/usr/bin/kdialog"))
            return await RunKdialog(title, startPath);
        return [];
    }

    private static Task<IReadOnlyList<string>> RunZenity(string title, string? startPath, bool allowMultiple) {
        var args = new List<string> {
            "--file-selection",
            "--directory",
            "--title=" + title
        };
        if (allowMultiple) {
            args.Add("--multiple");
            args.Add("--separator=\n");
        }

        if (!string.IsNullOrWhiteSpace(startPath))
            args.Add("--filename=" + startPath.TrimEnd('/') + "/");

        return RunPicker("/usr/bin/zenity", args, splitLines: allowMultiple);
    }

    private static Task<IReadOnlyList<string>> RunKdialog(string title, string? startPath) {
        var args = new List<string> { "--title", title, "--getexistingdirectory" };
        if (!string.IsNullOrWhiteSpace(startPath))
            args.Add(startPath);
        return RunPicker("/usr/bin/kdialog", args, splitLines: false);
    }

    private static async Task<IReadOnlyList<string>> RunPicker(string bin, List<string> args, bool splitLines) {
        try {
            var psi = new ProcessStartInfo(bin) {
                RedirectStandardOutput = true,
                UseShellExecute        = false
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process is null)
                return [];

            var output = (await process.StandardOutput.ReadToEndAsync()).Trim();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
                return [];

            IEnumerable<string> parts = splitLines
                ? output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : [output];

            return parts
                .Where(p => Directory.Exists(p))
                .Select(Path.GetFullPath)
                .ToList();
        }
        catch (Exception ex) {
            Debug.WriteLine($"{bin} failed: {ex.Message}");
            return [];
        }
    }
}
