using MapleWindow.Core.Notifications;

namespace MapleWindow.Core.Tests.Notifications;

public class NotificationPreferenceStoreTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"maplewindow-test-prefs-{Guid.NewGuid():N}.json");
    private const string OcidA = "캐릭터A";
    private const string OcidB = "캐릭터B";

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [Fact]
    public void Get_UnknownContent_ReturnsDefault()
    {
        var store = new NotificationPreferenceStore(_tempPath);

        var pref = store.Get(OcidA, "등록된적없는콘텐츠");

        Assert.False(pref.Muted);
        Assert.False(pref.OnceDailyOnly);
    }

    [Fact]
    public void SetThenGet_RoundTrips()
    {
        var store = new NotificationPreferenceStore(_tempPath);

        store.Set(OcidA, "몬스터 파크", new NotificationPreference(Muted: true, OnceDailyOnly: false));

        Assert.True(store.Get(OcidA, "몬스터 파크").Muted);
    }

    [Fact]
    public void Set_PersistsAcrossInstances()
    {
        new NotificationPreferenceStore(_tempPath).Set(OcidA, "듄켈", new NotificationPreference(Muted: false, OnceDailyOnly: true));

        var reloaded = new NotificationPreferenceStore(_tempPath);

        Assert.True(reloaded.Get(OcidA, "듄켈").OnceDailyOnly);
    }

    [Fact]
    public void Set_ForOneCharacter_DoesNotAffectAnotherCharacterWithSameContentName()
    {
        var store = new NotificationPreferenceStore(_tempPath);

        store.Set(OcidA, "몬스터 파크", new NotificationPreference(Muted: true, OnceDailyOnly: false));

        Assert.False(store.Get(OcidB, "몬스터 파크").Muted);
    }

    [Fact]
    public void GetAll_OnlyReturnsEntriesForRequestedCharacter()
    {
        var store = new NotificationPreferenceStore(_tempPath);
        store.Set(OcidA, "몬스터 파크", new NotificationPreference(Muted: true, OnceDailyOnly: false));
        store.Set(OcidB, "듄켈", new NotificationPreference(Muted: true, OnceDailyOnly: false));

        var all = store.GetAll(OcidA);

        Assert.True(all.ContainsKey("몬스터 파크"));
        Assert.False(all.ContainsKey("듄켈"));
    }
}
