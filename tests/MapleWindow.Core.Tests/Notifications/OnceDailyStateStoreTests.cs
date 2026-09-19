using MapleWindow.Core.Notifications;

namespace MapleWindow.Core.Tests.Notifications;

public class OnceDailyStateStoreTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"maplewindow-test-oncedaily-{Guid.NewGuid():N}.json");
    private static readonly DateTime Today = new(2026, 9, 21);

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [Fact]
    public void WasShownToday_NeverMarked_ReturnsFalse()
    {
        var store = new OnceDailyStateStore(_tempPath);

        Assert.False(store.WasShownToday("몬스터 파크", Today));
    }

    [Fact]
    public void MarkShown_ThenWasShownToday_ReturnsTrueForSameDate()
    {
        var store = new OnceDailyStateStore(_tempPath);

        store.MarkShown("몬스터 파크", Today);

        Assert.True(store.WasShownToday("몬스터 파크", Today));
    }

    [Fact]
    public void MarkShown_ThenWasShownToday_ReturnsFalseForDifferentDate()
    {
        var store = new OnceDailyStateStore(_tempPath);

        store.MarkShown("몬스터 파크", Today);

        Assert.False(store.WasShownToday("몬스터 파크", Today.AddDays(1)));
    }

    [Fact]
    public void MarkShown_PersistsAcrossInstances()
    {
        new OnceDailyStateStore(_tempPath).MarkShown("몬스터 파크", Today);

        var reloaded = new OnceDailyStateStore(_tempPath);

        Assert.True(reloaded.WasShownToday("몬스터 파크", Today));
    }
}
