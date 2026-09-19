using System.Text.Json;
using System.Text.Json.Serialization;

namespace MapleWindow.Core.Updates;

public sealed class GithubReleaseUpdateChecker : IUpdateChecker
{
    private const string RepoOwner = "ivealwaysbeen";
    private const string RepoName = "MapleWindow";
    private const string ZipAssetName = "MapleWindow-win-x64.zip";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;

    public GithubReleaseUpdateChecker(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.github.com");
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MapleWindow-UpdateChecker");
    }

    public async Task<GithubReleaseInfo?> GetLatestReleaseAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/repos/{RepoOwner}/{RepoName}/releases/latest", ct);
            if (!response.IsSuccessStatusCode) return null;

            await using var body = await response.Content.ReadAsStreamAsync(ct);
            var release = await JsonSerializer.DeserializeAsync<GithubReleaseApiResponse>(body, JsonOptions, ct);
            if (release?.TagName is null) return null;

            var asset = release.Assets?.FirstOrDefault(a => a.Name == ZipAssetName);
            if (asset?.BrowserDownloadUrl is null) return null;

            var versionText = release.TagName.TrimStart('v', 'V');
            if (!Version.TryParse(versionText, out var version)) return null;

            return new GithubReleaseInfo(version, asset.BrowserDownloadUrl);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            return null;
        }
    }

    private sealed record GithubReleaseApiResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        public List<GithubReleaseAssetApiResponse>? Assets { get; init; }
    }

    private sealed record GithubReleaseAssetApiResponse
    {
        public string? Name { get; init; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; init; }
    }
}
