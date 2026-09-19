namespace MapleWindow.Core.ImageCache;

public interface ICharacterImageCache
{
    /// <summary>Returns the PNG bytes for one action+frame combination of character_image, downloading and caching (memory + disk) on first request.</summary>
    Task<byte[]> GetFrameAsync(string characterImageBaseUrl, string actionCode, int frameIndex, CancellationToken ct = default);
}
