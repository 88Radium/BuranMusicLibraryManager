namespace Buran.Types;

public enum HostOsKind {
    Windows,
    Linux,
    MacOs,
    Other
}

public enum LinuxPackageKind {
    Rpm,
    Deb
}

public readonly record struct HostPlatform(HostOsKind Os, LinuxPackageKind? LinuxPackage) {
    public static HostPlatform Windows  { get; } = new(HostOsKind.Windows, null);
    public static HostPlatform MacOs    { get; } = new(HostOsKind.MacOs, null);
    public static HostPlatform LinuxRpm { get; } = new(HostOsKind.Linux, LinuxPackageKind.Rpm);
    public static HostPlatform LinuxDeb { get; } = new(HostOsKind.Linux, LinuxPackageKind.Deb);

    public static HostPlatform Detect() {
        if (OperatingSystem.IsWindows())
            return Windows;
        if (OperatingSystem.IsMacOS())
            return MacOs;
        if (OperatingSystem.IsLinux())
            return new HostPlatform(HostOsKind.Linux, LinuxPackageFromOsRelease(ReadOsRelease()));
        return new HostPlatform(HostOsKind.Other, null);
    }

    /// <summary>
    /// ID wins over ID_LIKE. Unknown Linux defaults to RPM (Fedora / Bazzite packages).
    /// </summary>
    public static LinuxPackageKind LinuxPackageFromOsRelease(string? text) {
        var id   = "";
        var like = "";
        if (!string.IsNullOrEmpty(text)) {
            foreach (var rawLine in text.Split('\n')) {
                var line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                var key   = line[..eq];
                var value = line[(eq + 1)..].Trim().Trim('"').Trim('\'');
                if (key == "ID")
                    id = value;
                else if (key == "ID_LIKE")
                    like = value;
            }
        }

        if (IsDebianFamily(id))
            return LinuxPackageKind.Deb;
        if (IsRpmFamily(id))
            return LinuxPackageKind.Rpm;
        if (IsDebianFamily(like))
            return LinuxPackageKind.Deb;
        if (IsRpmFamily(like))
            return LinuxPackageKind.Rpm;
        return LinuxPackageKind.Rpm;
    }

    private static string ReadOsRelease() {
        try {
            if (File.Exists("/etc/os-release"))
                return File.ReadAllText("/etc/os-release");
            if (File.Exists("/usr/lib/os-release"))
                return File.ReadAllText("/usr/lib/os-release");
        }
        catch {
            // Unreadable os-release still gets RPM.
        }

        return "";
    }

    private static bool IsDebianFamily(string value) =>
        ContainsToken(value, "debian", "ubuntu", "linuxmint", "pop", "elementary", "raspbian", "kali", "zorin", "mx");

    private static bool IsRpmFamily(string value) =>
        ContainsToken(value, "fedora", "rhel", "centos", "rocky", "alma", "almalinux", "bazzite", "nobara",
            "mageia", "opensuse", "sles", "sled", "ol");

    private static bool ContainsToken(string haystack, params string[] tokens) {
        if (string.IsNullOrWhiteSpace(haystack))
            return false;
        foreach (var part in haystack.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)) {
            foreach (var token in tokens) {
                if (part.Equals(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}

public sealed record ReleaseAsset(string Name, string Url);

public enum UpdateKind {
    Failed,
    UpToDate,
    Available,
    NewerWithoutInstaller
}

public sealed class UpdateDecision {
    public UpdateKind     Kind      { get; init; }
    public Version        Installed { get; init; } = new(0, 0, 0);
    public Version?       Remote    { get; init; }
    public HostPlatform   Platform  { get; init; }
    public ReleaseAsset?  Asset     { get; init; }

    public string  InstalledDisplay => Format(Installed);
    public string? RemoteDisplay    => Remote is null ? null : Format(Remote);

    public static string Format(Version version) {
        var build = version.Build < 0 ? 0 : version.Build;
        return $"{version.Major}.{version.Minor}.{build}";
    }
}

public static class UpdateCheck {
    public static Version ParseVersionOrZero(string? raw) =>
        TryParseVersion(raw, out var version) ? version : new Version(0, 0, 0);

    public static bool TryParseVersion(string? raw, out Version version) {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim();
        if (text.StartsWith('v') || text.StartsWith('V'))
            text = text[1..];

        var cut = text.IndexOfAny(['+', '-']);
        if (cut >= 0)
            text = text[..cut];

        if (!Version.TryParse(text, out var parsed))
            return false;

        version = ThreePart(parsed);
        return true;
    }

    public static Version ThreePart(Version version) =>
        new(version.Major, version.Minor, version.Build < 0 ? 0 : version.Build);

    public static UpdateDecision Decide(
        Version installed,
        string? tagName,
        bool prerelease,
        bool draft,
        IReadOnlyList<ReleaseAsset> assets,
        HostPlatform platform) {
        installed = ThreePart(installed);
        if (prerelease || draft)
            return new UpdateDecision { Kind = UpdateKind.UpToDate, Installed = installed, Platform = platform };

        if (!TryParseVersion(tagName, out var remote))
            return new UpdateDecision { Kind = UpdateKind.Failed, Installed = installed, Platform = platform };

        if (remote <= installed) {
            return new UpdateDecision {
                Kind      = UpdateKind.UpToDate,
                Installed = installed,
                Remote    = remote,
                Platform  = platform
            };
        }

        var asset = PickAsset(assets, platform);
        if (asset is null) {
            return new UpdateDecision {
                Kind      = UpdateKind.NewerWithoutInstaller,
                Installed = installed,
                Remote    = remote,
                Platform  = platform
            };
        }

        return new UpdateDecision {
            Kind      = UpdateKind.Available,
            Installed = installed,
            Remote    = remote,
            Platform  = platform,
            Asset     = asset
        };
    }

    public static ReleaseAsset? PickAsset(IEnumerable<ReleaseAsset> assets, HostPlatform platform) {
        var list = assets
            .Where(a => !string.IsNullOrWhiteSpace(a.Name) && !string.IsNullOrWhiteSpace(a.Url))
            .ToList();

        return platform.Os switch {
            HostOsKind.Windows =>
                First(list, n => n.EndsWith("-win-x64-setup.exe", StringComparison.OrdinalIgnoreCase))
                ?? First(list, n => n.EndsWith("setup.exe", StringComparison.OrdinalIgnoreCase))
                ?? First(list, n => n.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                ?? First(list, n => n.Contains("win", StringComparison.OrdinalIgnoreCase)
                                    && n.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)),
            HostOsKind.Linux when platform.LinuxPackage == LinuxPackageKind.Deb =>
                First(list, n => n.EndsWith(".deb", StringComparison.OrdinalIgnoreCase)
                                 && !n.Contains("dbgsym", StringComparison.OrdinalIgnoreCase)),
            HostOsKind.Linux =>
                First(list, n => n.EndsWith(".rpm", StringComparison.OrdinalIgnoreCase)
                                 && !n.Contains(".src.", StringComparison.OrdinalIgnoreCase)
                                 && !n.Contains("debug", StringComparison.OrdinalIgnoreCase)),
            _ => null
        };
    }

    private static ReleaseAsset? First(List<ReleaseAsset> assets, Func<string, bool> nameMatches) =>
        assets.FirstOrDefault(a => nameMatches(a.Name));
}
