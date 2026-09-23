using Buran.Types;

namespace Buran.Tests;

public class UpdateCheckTests {
    private static readonly ReleaseAsset[] SampleAssets = [
        new("buran-0.1.9-4.x86_64.rpm", "https://example/buran.rpm"),
        new("Buran-0.1.9-win-x64-setup.exe", "https://example/setup.exe"),
        new("Buran-0.1.9-win-x64.zip", "https://example/portable.zip"),
        new("buran_0.1.9-4_amd64.deb", "https://example/buran.deb")
    ];

    [Theory]
    [InlineData("v0.1.8", "0.1.8")]
    [InlineData("0.1.8", "0.1.8")]
    [InlineData("0.1.8+abc", "0.1.8")]
    [InlineData("V0.1.9", "0.1.9")]
    [InlineData("0.1.8.0", "0.1.8")]
    public void TagAndInformationalVersion_ParseToThreeParts(string raw, string expected) {
        Assert.True(UpdateCheck.TryParseVersion(raw, out var version));
        Assert.Equal(expected, UpdateDecision.Format(version));
    }

    [Fact]
    public void SameVersion_IsUpToDate() {
        var decision = UpdateCheck.Decide(
            new Version(0, 1, 8), "v0.1.8", false, false, SampleAssets, HostPlatform.LinuxRpm);

        Assert.Equal(UpdateKind.UpToDate, decision.Kind);
        Assert.Null(decision.Asset);
    }

    [Fact]
    public void NewerLinuxRpm_PicksRpmNotDebOrWindows() {
        var decision = UpdateCheck.Decide(
            new Version(0, 1, 8), "v0.1.9", false, false, SampleAssets, HostPlatform.LinuxRpm);

        Assert.Equal(UpdateKind.Available, decision.Kind);
        Assert.Equal("buran-0.1.9-4.x86_64.rpm", decision.Asset?.Name);
        Assert.Equal("https://example/buran.rpm", decision.Asset?.Url);
        Assert.Equal("0.1.9", decision.RemoteDisplay);
    }

    [Fact]
    public void NewerLinuxDeb_PicksDeb() {
        var decision = UpdateCheck.Decide(
            new Version(0, 1, 8), "v0.1.9", false, false, SampleAssets, HostPlatform.LinuxDeb);

        Assert.Equal("buran_0.1.9-4_amd64.deb", decision.Asset?.Name);
    }

    [Fact]
    public void NewerWindows_PrefersSetupExeOverZip() {
        var decision = UpdateCheck.Decide(
            new Version(0, 1, 8), "v0.1.9", false, false, SampleAssets, HostPlatform.Windows);

        Assert.Equal("Buran-0.1.9-win-x64-setup.exe", decision.Asset?.Name);
    }

    [Fact]
    public void NewerMac_HasNoInstaller() {
        var decision = UpdateCheck.Decide(
            new Version(0, 1, 8), "v0.1.9", false, false, SampleAssets, HostPlatform.MacOs);

        Assert.Equal(UpdateKind.NewerWithoutInstaller, decision.Kind);
        Assert.Null(decision.Asset);
        Assert.Equal("0.1.9", decision.RemoteDisplay);
    }

    [Fact]
    public void NewerLinuxRpm_WithoutRpmAsset_HasNoInstaller() {
        ReleaseAsset[] windowsOnly = [
            new("Buran-0.1.9-win-x64-setup.exe", "https://example/setup.exe")
        ];

        var decision = UpdateCheck.Decide(
            new Version(0, 1, 8), "v0.1.9", false, false, windowsOnly, HostPlatform.LinuxRpm);

        Assert.Equal(UpdateKind.NewerWithoutInstaller, decision.Kind);
    }

    [Fact]
    public void DraftOrPrerelease_IsNotAnUpdate() {
        var draft = UpdateCheck.Decide(
            new Version(0, 1, 8), "v0.1.9", false, true, SampleAssets, HostPlatform.LinuxRpm);
        var pre = UpdateCheck.Decide(
            new Version(0, 1, 8), "v0.1.9", true, false, SampleAssets, HostPlatform.LinuxRpm);

        Assert.Equal(UpdateKind.UpToDate, draft.Kind);
        Assert.Equal(UpdateKind.UpToDate, pre.Kind);
    }

    [Fact]
    public void BadTag_Fails() {
        var decision = UpdateCheck.Decide(
            new Version(0, 1, 8), "not-a-version", false, false, SampleAssets, HostPlatform.LinuxRpm);

        Assert.Equal(UpdateKind.Failed, decision.Kind);
    }

    [Fact]
    public void BazziteOsRelease_IsRpm() {
        const string text = """
            NAME="Bazzite"
            ID=bazzite
            ID_LIKE="rhel centos fedora"
            """;

        Assert.Equal(LinuxPackageKind.Rpm, HostPlatform.LinuxPackageFromOsRelease(text));
    }

    [Fact]
    public void UbuntuOsRelease_IsDeb() {
        const string text = """
            ID=ubuntu
            ID_LIKE=debian
            """;

        Assert.Equal(LinuxPackageKind.Deb, HostPlatform.LinuxPackageFromOsRelease(text));
    }

    [Fact]
    public void EmptyOsRelease_DefaultsToRpm() {
        Assert.Equal(LinuxPackageKind.Rpm, HostPlatform.LinuxPackageFromOsRelease(""));
    }
}
