using System.Security.Cryptography;
using System.Text;

namespace MapleWindow.Core.ImageCache;

/// <summary>
/// character_image supports ?action=A02.1-style query params per Nexon's docs (confirmed, not guessed).
/// Assumes the static image URL does NOT need the x-nxopen-api-key header (it's a static asset path,
/// distinct from the /v1/... API endpoints) — flagged in the plan as needing live-run confirmation;
/// if wrong, this is a one-line fix (add the header below).
/// </summary>
public sealed class CharacterImageCache : ICharacterImageCache
{
    private readonly HttpClient _httpClient;
    private readonly string _diskCacheDirectory;
    private readonly Dictionary<string, byte[]> _memoryCache = new();

    public CharacterImageCache(HttpClient httpClient, string? diskCacheDirectory = null)
    {
        _httpClient = httpClient;
        _diskCacheDirectory = diskCacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MapleWindow", "imagecache");
    }

    public async Task<byte[]> GetFrameAsync(string characterImageBaseUrl, string actionCode, int frameIndex, CancellationToken ct = default)
    {
        var url = BuildUrl(characterImageBaseUrl, actionCode, frameIndex);

        if (_memoryCache.TryGetValue(url, out var cached))
            return cached;

        var diskPath = Path.Combine(_diskCacheDirectory, HashUrl(url) + ".png");
        if (File.Exists(diskPath))
        {
            var fromDisk = await File.ReadAllBytesAsync(diskPath, ct).ConfigureAwait(false);
            _memoryCache[url] = fromDisk;
            return fromDisk;
        }

        var downloaded = await _httpClient.GetByteArrayAsync(url, ct).ConfigureAwait(false);
        _memoryCache[url] = downloaded;

        Directory.CreateDirectory(_diskCacheDirectory);
        await File.WriteAllBytesAsync(diskPath, downloaded, ct).ConfigureAwait(false);

        return downloaded;
    }

    internal static string BuildUrl(string baseUrl, string actionCode, int frameIndex)
    {
        // character_image already carries its own query string (observed: "...?wmotion=W00") — appending
        // another "?action=..." instead of "&action=..." produced a malformed URL where the server silently
        // ignored the action/frame and always returned the same default pose (confirmed: every cached frame
        // came back byte-identical regardless of requested action).
        var separator = baseUrl.Contains('?') ? '&' : '?';
        return $"{baseUrl}{separator}action={actionCode}.{frameIndex}";
    }

    private static string HashUrl(string url)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)));
}
