using System.Net;
using System.Text;
using MapleWindow.Core.Tests.TestDoubles;
using MapleWindow.Core.Updates;

namespace MapleWindow.Core.Tests.Updates;

public class GithubReleaseUpdateCheckerTests
{
    private static GithubReleaseUpdateChecker CreateChecker(FakeHttpMessageHandler handler)
        => new(new HttpClient(handler));

    [Fact]
    public async Task GetLatestReleaseAsync_ValidReleaseWithMatchingAsset_ReturnsVersionAndUrl()
    {
        var handler = new FakeHttpMessageHandler
        {
            ResponseBytes = Encoding.UTF8.GetBytes("""
                {
                  "tag_name": "v1.2.3",
                  "assets": [
                    { "name": "MapleWindow-win-x64.zip", "browser_download_url": "https://github.com/ivealwaysbeen/MapleWindow/releases/download/v1.2.3/MapleWindow-win-x64.zip" }
                  ]
                }
                """),
        };
        var checker = CreateChecker(handler);

        var result = await checker.GetLatestReleaseAsync();

        Assert.NotNull(result);
        Assert.Equal(new Version(1, 2, 3), result!.Version);
        Assert.Equal("https://github.com/ivealwaysbeen/MapleWindow/releases/download/v1.2.3/MapleWindow-win-x64.zip", result.ZipAssetUrl);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_RequestsCorrectEndpoint()
    {
        var handler = new FakeHttpMessageHandler
        {
            ResponseBytes = Encoding.UTF8.GetBytes("""{ "tag_name": "v1.0.0", "assets": [] }"""),
        };
        var checker = CreateChecker(handler);

        await checker.GetLatestReleaseAsync();

        Assert.Equal(["https://api.github.com/repos/ivealwaysbeen/MapleWindow/releases/latest"], handler.RequestedUrls);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_NoMatchingAsset_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler
        {
            ResponseBytes = Encoding.UTF8.GetBytes("""
                { "tag_name": "v1.2.3", "assets": [ { "name": "other-file.zip", "browser_download_url": "https://example.com/other-file.zip" } ] }
                """),
        };
        var checker = CreateChecker(handler);

        var result = await checker.GetLatestReleaseAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_MalformedJson_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler { ResponseBytes = Encoding.UTF8.GetBytes("not json") };
        var checker = CreateChecker(handler);

        var result = await checker.GetLatestReleaseAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_HttpErrorStatus_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler { StatusCode = HttpStatusCode.InternalServerError };
        var checker = CreateChecker(handler);

        var result = await checker.GetLatestReleaseAsync();

        Assert.Null(result);
    }
}
