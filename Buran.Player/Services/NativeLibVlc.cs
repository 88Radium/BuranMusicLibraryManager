using System.Reflection;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;

namespace Buran.Player.Services;

internal static class NativeLibVlc {
    private static bool _resolverRegistered;

    public static string AppDir => AppContext.BaseDirectory;

    public static string[] LibVlcOptions() {
        var pluginPath = Path.Combine(AppDir, "vlc", "plugins");
        if (Directory.Exists(pluginPath))
            return ["--no-video", "--quiet", $"--plugin-path={pluginPath}"];
        return ["--no-video", "--quiet"];
    }

    public static void Prepare() {
        var pluginPath = Path.Combine(AppDir, "vlc", "plugins");
        if (Directory.Exists(pluginPath))
            Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginPath);

        RegisterResolver();
        Preload("libvlccore.so.9", "libvlccore.so");
        Preload("libvlc.so.5", "libvlc.so");
        Core.Initialize();
    }

    private static void RegisterResolver() {
        if (_resolverRegistered)
            return;
        _resolverRegistered = true;
        try {
            NativeLibrary.SetDllImportResolver(typeof(Core).Assembly, Resolve);
        }
        catch (InvalidOperationException) {
            // Resolver already set for this assembly.
        }
    }

    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
        var path = libraryName switch {
            "libvlc" or "vlc" or "libvlc.so" or "libvlc.so.5" =>
                FirstExisting("libvlc.so", "libvlc.so.5"),
            "libvlccore" or "libvlccore.so" or "libvlccore.so.9" =>
                FirstExisting("libvlccore.so", "libvlccore.so.9"),
            _ => null
        };
        return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
    }

    private static void Preload(params string[] names) {
        var path = FirstExisting(names);
        if (path is null)
            return;
        try {
            NativeLibrary.Load(path);
        }
        catch {
            // Prepare() still calls Core.Initialize(); the error surfaces there.
        }
    }

    private static string? FirstExisting(params string[] names) {
        foreach (var name in names) {
            var path = Path.Combine(AppDir, name);
            if (File.Exists(path))
                return path;
        }

        return null;
    }
}
