using System.Reflection;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;

namespace Buran.Player.Services;

internal static class NativeLibVlc {
    private static bool _resolverRegistered;

    public static string AppDir => AppContext.BaseDirectory;

    public static string[] LibVlcOptions() => ["--no-video", "--quiet"];

    public static void Prepare() {
        var pluginPath = FindPluginDir();
        if (pluginPath is not null)
            Environment.SetEnvironmentVariable("VLC_PLUGIN_PATH", pluginPath);

        RegisterResolver();
        Preload("libvlccore.so.9", "libvlccore.so", "libvlccore.dll");
        Preload("libvlc.so.5", "libvlc.so", "libvlc.dll");
        // LibVLCSharp.Core.Initialize(directory) throws on Linux even when the
        // copy lives next to BuranUI. Bundled libs are found via the resolver,
        // LD_LIBRARY_PATH (buran launcher), and VLC_PLUGIN_PATH.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || !BundledLibVlc())
            Core.Initialize();
        else
            Core.Initialize(AppDir);
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
            "libvlc" or "vlc" or "libvlc.so" or "libvlc.so.5" or "libvlc.dll" =>
                FirstExisting("libvlc.so", "libvlc.so.5", "libvlc.dll"),
            "libvlccore" or "libvlccore.so" or "libvlccore.so.9" or "libvlccore.dll" =>
                FirstExisting("libvlccore.so", "libvlccore.so.9", "libvlccore.dll"),
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

    private static bool BundledLibVlc() =>
        File.Exists(Path.Combine(AppDir, "libvlc.so")) ||
        File.Exists(Path.Combine(AppDir, "libvlc.so.5")) ||
        File.Exists(Path.Combine(AppDir, "libvlc.dll"));

    private static IEnumerable<string> LibDirs() {
        yield return AppDir;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            yield break;
        yield return "/usr/lib64";
        yield return "/usr/lib/x86_64-linux-gnu";
        yield return "/usr/lib";
        yield return "/lib64";
        yield return "/lib/x86_64-linux-gnu";
    }

    private static string? FirstExisting(params string[] names) {
        foreach (var dir in LibDirs()) {
            foreach (var name in names) {
                var path = Path.Combine(dir, name);
                if (File.Exists(path))
                    return path;
            }
        }

        return null;
    }

    private static string? FindPluginDir() {
        foreach (var dir in PluginDirs()) {
            if (Directory.Exists(dir))
                return dir;
        }

        return null;
    }

    private static IEnumerable<string> PluginDirs() {
        yield return Path.Combine(AppDir, "vlc", "plugins");
        yield return Path.Combine(AppDir, "plugins");
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            yield break;
        yield return "/usr/lib64/vlc/plugins";
        yield return "/usr/lib/x86_64-linux-gnu/vlc/plugins";
        yield return "/usr/lib/vlc/plugins";
    }
}
