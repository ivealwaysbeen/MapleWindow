using MapleWindow.Core.Notifications;
using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Tests.TestDoubles;

namespace MapleWindow.Core.Tests.Scheduler;

public class SpeakCycleServiceTests
{
    // Monday 2026-09-21 (verified), day-of-month 21.
    private static readonly DateTime Monday21st = new(2026, 9, 21);

    private static ScheduledContentPool FullPool() => new()
    {
        DailyContents =
        [
            new ScheduledContentItem("일퀘A", "quest", 0, 1, "0", false),
            new ScheduledContentItem("몬스터 파크", "contents", 0, 5, "0", false),
        ],
        WeeklyContents = [new ScheduledContentItem("위클리A", "contents", 1, 5, "0", false)],
        BossContents =
        [
            new ScheduledBossItem("보스위클리A", "노멀", "bossWeekly", false),
            new ScheduledBossItem("보스먼슬리A", "노멀", "bossMonthly", false),
        ],
    };

    private static FakePhraseRepository RepositoryWithAllKeys()
    {
        var repository = new FakePhraseRepository();
        repository.Set("daily.quest.allUndone", "PHRASE_DAILY_QUEST");
        repository.Set("daily.monsterPark.notStarted", "PHRASE_DAILY_MONSTERPARK");
        repository.Set("weekly.content.monToWed", "PHRASE_WEEKLY");
        repository.Set("boss.weekly.allFalse.sunToWed", "PHRASE_BOSS_WEEKLY");
        repository.Set("boss.monthly.late", "PHRASE_BOSS_MONTHLY");
        return repository;
    }

    private const string Ocid = "캐릭터A";

    private static SpeakCycleService BuildService(ScheduledContentPool pool, FakePhraseRepository repository, DateTime now)
        => new(() => pool, () => Ocid, repository, new FakeNotificationPreferenceStore(), new FakeOnceDailyStateStore(), () => now);

    [Fact]
    public void EvaluateAndRaise_OrdersMessages_DailyThenWeeklyThenBossWeeklyThenBossMonthly()
    {
        var service = BuildService(FullPool(), RepositoryWithAllKeys(), Monday21st);

        var result = service.EvaluateAndRaise();

        Assert.Equal(
            ["PHRASE_DAILY_QUEST", "PHRASE_DAILY_MONSTERPARK", "PHRASE_WEEKLY", "PHRASE_BOSS_WEEKLY", "PHRASE_BOSS_MONTHLY"],
            result.Select(r => r.Text).ToList());
    }

    [Fact]
    public void EvaluateAndRaise_EmptyPool_ReturnsEmpty_NoEventRaised()
    {
        var service = BuildService(new ScheduledContentPool(), RepositoryWithAllKeys(), Monday21st);
        var eventRaised = false;
        service.QueueReady += (_, _) => eventRaised = true;

        var result = service.EvaluateAndRaise();

        Assert.Empty(result);
        Assert.False(eventRaised);
    }

    [Fact]
    public void EvaluateAndRaise_NonEmptyResult_RaisesQueueReadyEvent()
    {
        var service = BuildService(FullPool(), RepositoryWithAllKeys(), Monday21st);
        IReadOnlyList<ResolvedSpeech>? raised = null;
        service.QueueReady += (_, queue) => raised = queue;

        service.EvaluateAndRaise();

        Assert.NotNull(raised);
        Assert.Equal(5, raised!.Count);
    }

    [Fact]
    public void EvaluateAndRaise_MissingPhraseTemplate_SkipsThatMessageOnly()
    {
        var repository = RepositoryWithAllKeys();
        // Remove the weekly key's coverage by using a fresh repository without it.
        var partial = new FakePhraseRepository();
        partial.Set("daily.quest.allUndone", "PHRASE_DAILY_QUEST");
        // weekly/monsterPark/boss keys intentionally left unset.

        var service = BuildService(FullPool(), partial, Monday21st);

        var result = service.EvaluateAndRaise();

        Assert.Single(result);
        Assert.Equal("PHRASE_DAILY_QUEST", result[0].Text);
    }

    [Fact]
    public void EvaluateAndRaise_MutedContent_ExcludedFromQueue()
    {
        var preferences = new FakeNotificationPreferenceStore();
        preferences.Set(Ocid, "위클리A", new NotificationPreference(Muted: true, OnceDailyOnly: false));
        var service = new SpeakCycleService(FullPool, () => Ocid, RepositoryWithAllKeys(), preferences, new FakeOnceDailyStateStore(), () => Monday21st);

        var result = service.EvaluateAndRaise();

        Assert.DoesNotContain(result, r => r.Text == "PHRASE_WEEKLY");
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void UpdateInterval_RaisesIntervalChanged_SoDisplayedSpeechCanBeCleared()
    {
        var service = BuildService(FullPool(), RepositoryWithAllKeys(), Monday21st);
        service.Start(TimeSpan.FromMinutes(30));
        var raised = false;
        service.IntervalChanged += (_, _) => raised = true;

        service.UpdateInterval(TimeSpan.FromSeconds(5));

        Assert.True(raised);
    }
}
