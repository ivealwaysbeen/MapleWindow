using MapleWindow.Core.ImageCache;
using MapleWindow.Core.Tests.TestDoubles;

namespace MapleWindow.Core.Tests.ImageCache;

public class CharacterImageCacheTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"maplewindow-test-imagecache-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task GetFrameAsync_BuildsUrlWithActionAndFrameQueryParams()
    {
        var handler = new FakeHttpMessageHandler();
        var cache = new CharacterImageCache(new HttpClient(handler), _tempDir);

        await cache.GetFrameAsync("https://open.api.nexon.com/static/maplestory/character/look/ABC", "A02", 1);

        Assert.Equal(["https://open.api.nexon.com/static/maplestory/character/look/ABC?action=A02.1"], handler.RequestedUrls);
    }

    [Fact]
    public async Task GetFrameAsync_BaseUrlAlreadyHasQueryString_UsesAmpersandNotSecondQuestionMark()
    {
        // Real character_image values come back as "...look/<id>?wmotion=W00" — a second "?" here would
        // produce a malformed URL where the server ignores action/frame and always returns the default pose
        // (confirmed against the live API: every requested action/frame came back byte-identical until this
        // was fixed to use "&").
        var handler = new FakeHttpMessageHandler();
        var cache = new CharacterImageCache(new HttpClient(handler), _tempDir);

        await cache.GetFrameAsync("https://open.api.nexon.com/static/maplestory/character/look/ABC?wmotion=W00", "A02", 1);

        Assert.Equal(["https://open.api.nexon.com/static/maplestory/character/look/ABC?wmotion=W00&action=A02.1"], handler.RequestedUrls);
    }

    [Fact]
    public async Task GetFrameAsync_ReturnsDownloadedBytes()
    {
        var handler = new FakeHttpMessageHandler { ResponseBytes = [9, 8, 7] };
        var cache = new CharacterImageCache(new HttpClient(handler), _tempDir);

        var bytes = await cache.GetFrameAsync("https://example.test/img", "A00", 0);

        Assert.Equal(new byte[] { 9, 8, 7 }, bytes);
    }

    [Fact]
    public async Task GetFrameAsync_SecondCallSameFrame_ServedFromMemory_NoSecondDownload()
    {
        var handler = new FakeHttpMessageHandler();
        var cache = new CharacterImageCache(new HttpClient(handler), _tempDir);

        await cache.GetFrameAsync("https://example.test/img", "A00", 0);
        await cache.GetFrameAsync("https://example.test/img", "A00", 0);

        Assert.Single(handler.RequestedUrls);
    }

    [Fact]
    public async Task GetFrameAsync_DifferentFrame_TriggersSeparateDownload()
    {
        var handler = new FakeHttpMessageHandler();
        var cache = new CharacterImageCache(new HttpClient(handler), _tempDir);

        await cache.GetFrameAsync("https://example.test/img", "A00", 0);
        await cache.GetFrameAsync("https://example.test/img", "A00", 1);

        Assert.Equal(2, handler.RequestedUrls.Count);
    }

    [Fact]
    public async Task GetFrameAsync_AfterRestart_ServedFromDiskCache_NoNetworkCall()
    {
        var firstHandler = new FakeHttpMessageHandler { ResponseBytes = [5, 5, 5] };
        var firstCache = new CharacterImageCache(new HttpClient(firstHandler), _tempDir);
        await firstCache.GetFrameAsync("https://example.test/img", "A02", 2);

        // Simulate an app restart: a brand-new cache instance (empty memory cache) pointed at the same disk directory.
        var secondHandler = new FakeHttpMessageHandler();
        var secondCache = new CharacterImageCache(new HttpClient(secondHandler), _tempDir);
        var bytes = await secondCache.GetFrameAsync("https://example.test/img", "A02", 2);

        Assert.Empty(secondHandler.RequestedUrls);
        Assert.Equal(new byte[] { 5, 5, 5 }, bytes);
    }
}
