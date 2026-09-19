using MapleWindow.Core.Notifications;
using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Tests.TestDoubles;

namespace MapleWindow.Core.Tests.Notifications;

public class NotificationFilterTests
{
    private static readonly DateTime Today = new(2026, 9, 21);

    [Fact]
    public void SingleItemMessage_Muted_Excluded()
    {
        var prefs = new FakeNotificationPreferenceStore();
        prefs.Set("몬스터 파크", new NotificationPreference(Muted: true, OnceDailyOnly: false));
        var message = new PhraseMessage(["몬스터 파크"], "daily.monsterPark.notStarted", new Dictionary<string, string>());

        var result = NotificationFilter.Apply([message], prefs, new FakeOnceDailyStateStore(), Today);

        Assert.Empty(result);
    }

    [Fact]
    public void SingleItemMessage_NotMuted_Included()
    {
        var message = new PhraseMessage(["몬스터 파크"], "daily.monsterPark.notStarted", new Dictionary<string, string>());

        var result = NotificationFilter.Apply([message], new FakeNotificationPreferenceStore(), new FakeOnceDailyStateStore(), Today);

        Assert.Single(result);
    }

    [Fact]
    public void GroupMessage_OnlySomeMembersMuted_StillIncluded()
    {
        var prefs = new FakeNotificationPreferenceStore();
        prefs.Set("퀘스트A", new NotificationPreference(Muted: true, OnceDailyOnly: false));
        var message = new PhraseMessage(["퀘스트A", "퀘스트B"], "daily.quest.allUndone", new Dictionary<string, string>());

        var result = NotificationFilter.Apply([message], prefs, new FakeOnceDailyStateStore(), Today);

        Assert.Single(result);
    }

    [Fact]
    public void GroupMessage_AllMembersMuted_Excluded()
    {
        var prefs = new FakeNotificationPreferenceStore();
        prefs.Set("퀘스트A", new NotificationPreference(Muted: true, OnceDailyOnly: false));
        prefs.Set("퀘스트B", new NotificationPreference(Muted: true, OnceDailyOnly: false));
        var message = new PhraseMessage(["퀘스트A", "퀘스트B"], "daily.quest.allUndone", new Dictionary<string, string>());

        var result = NotificationFilter.Apply([message], prefs, new FakeOnceDailyStateStore(), Today);

        Assert.Empty(result);
    }

    [Fact]
    public void OnceDailyOnly_NotYetShownToday_IncludedAndMarksShown()
    {
        var prefs = new FakeNotificationPreferenceStore();
        prefs.Set("몬스터 파크", new NotificationPreference(Muted: false, OnceDailyOnly: true));
        var onceDaily = new FakeOnceDailyStateStore();
        var message = new PhraseMessage(["몬스터 파크"], "daily.monsterPark.notStarted", new Dictionary<string, string>());

        var result = NotificationFilter.Apply([message], prefs, onceDaily, Today);

        Assert.Single(result);
        Assert.True(onceDaily.WasShownToday("몬스터 파크", Today));
    }

    [Fact]
    public void OnceDailyOnly_AlreadyShownToday_Excluded()
    {
        var prefs = new FakeNotificationPreferenceStore();
        prefs.Set("몬스터 파크", new NotificationPreference(Muted: false, OnceDailyOnly: true));
        var onceDaily = new FakeOnceDailyStateStore();
        onceDaily.MarkShown("몬스터 파크", Today);
        var message = new PhraseMessage(["몬스터 파크"], "daily.monsterPark.notStarted", new Dictionary<string, string>());

        var result = NotificationFilter.Apply([message], prefs, onceDaily, Today);

        Assert.Empty(result);
    }

    [Fact]
    public void OnceDailyOnly_NewDay_IncludedAgain()
    {
        var prefs = new FakeNotificationPreferenceStore();
        prefs.Set("몬스터 파크", new NotificationPreference(Muted: false, OnceDailyOnly: true));
        var onceDaily = new FakeOnceDailyStateStore();
        onceDaily.MarkShown("몬스터 파크", Today);
        var message = new PhraseMessage(["몬스터 파크"], "daily.monsterPark.notStarted", new Dictionary<string, string>());

        var result = NotificationFilter.Apply([message], prefs, onceDaily, Today.AddDays(1));

        Assert.Single(result);
    }

    [Fact]
    public void OnceDailyOnly_DoesNotApplyToGroupMessages()
    {
        var prefs = new FakeNotificationPreferenceStore();
        prefs.Set("퀘스트A", new NotificationPreference(Muted: false, OnceDailyOnly: true));
        var onceDaily = new FakeOnceDailyStateStore();
        onceDaily.MarkShown("퀘스트A", Today);
        var message = new PhraseMessage(["퀘스트A", "퀘스트B"], "daily.quest.allUndone", new Dictionary<string, string>());

        var result = NotificationFilter.Apply([message], prefs, onceDaily, Today);

        Assert.Single(result);
    }
}
