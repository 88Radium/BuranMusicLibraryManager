using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Buran.Types;

namespace BuranUI.Services;

public static class GitHubUpdateClient {
    public const string LatestReleaseUrl =
        "https://api.github.com/repos/88Radium/BuranMusicLibraryManager/releases/latest";

    private static readonly HttpClient Http = CreateClient();

    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<GitHubLatestRelease?> FetchLatestAsync(CancellationToken cancellationToken = default) {
        try {
            using var response = await Http.GetAsync(LatestReleaseUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var dto  = JsonSerializer.Deserialize<ReleaseDto>(json, JsonOptions);
            if (dto is null)
                return null;

            return new GitHubLatestRelease {
                TagName    = dto.TagName,
                Prerelease = dto.Prerelease,
                Draft      = dto.Draft,
                Assets     = dto.Assets
                    .Where(a => !string.IsNullOrWhiteSpace(a.Name) && !string.IsNullOrWhiteSpace(a.BrowserDownloadUrl))
                    .Select(a => new ReleaseAsset(a.Name!, a.BrowserDownloadUrl!))
                    .ToList()
            };
        }
        catch (Exception) {
            return null;
        }
    }

    private static HttpClient CreateClient() {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        // GitHub rejects requests without a User-Agent.
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("BuranMusicLibraryManager", AppVersion.Display));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private sealed class ReleaseDto {
        [JsonPropertyName("tag_name")]   public string?     TagName    { get; set; }
        [JsonPropertyName("prerelease")] public bool        Prerelease { get; set; }
        [JsonPropertyName("draft")]      public bool        Draft      { get; set; }
        [JsonPropertyName("assets")]     public AssetDto[]  Assets     { get; set; } = [];
    }

    private sealed class AssetDto {
        [JsonPropertyName("name")]                  public string? Name                { get; set; }
        [JsonPropertyName("browser_download_url")]  public string? BrowserDownloadUrl  { get; set; }
    }
}

public sealed class GitHubLatestRelease {
    public string?                      TagName    { get; init; }
    public bool                         Prerelease { get; init; }
    public bool                         Draft      { get; init; }
    public IReadOnlyList<ReleaseAsset>  Assets     { get; init; } = [];
}
