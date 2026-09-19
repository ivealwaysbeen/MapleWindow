using MapleWindow.Core.Notifications;

namespace MapleWindow.Core.Tests.Notifications;

public class NotificationPreferenceStoreTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"maplewindow-test-prefs-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [Fact]
    public void Get_UnknownContent_ReturnsDefault()
    {
        var store = new NotificationPreferenceStore(_tempPath);

        var pref = store.Get("등록된적없는콘텐츠");

        Assert.False(pref.Muted);
        Assert.False(pref.OnceDailyOnly);
    }

    [Fact]
    public void SetThenGet_RoundTrips()
    {
        var store = new NotificationPreferenceStore(_tempPath);

        store.Set("몬스터 파크", new NotificationPreference(Muted: true, OnceDailyOnly: false));

        Assert.True(store.Get("몬스터 파크").Muted);
    }

    [Fact]
    public void Set_PersistsAcrossInstances()
    {
        new NotificationPreferenceStore(_tempPath).Set("듄켈", new NotificationPreference(Muted: false, OnceDailyOnly: true));

        var reloaded = new NotificationPreferenceStore(_tempPath);

        Assert.True(reloaded.Get("듄켈").OnceDailyOnly);
    }
}
