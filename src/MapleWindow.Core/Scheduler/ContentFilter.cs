using MapleWindow.Core.Nexon.Models;

namespace MapleWindow.Core.Scheduler;

/// <summary>Filters a raw scheduler API response down to registration_flag=="true" items and normalizes daily/weekly/boss into one pool shape.</summary>
public static class ContentFilter
{
    private const string RegisteredTrue = "true";

    /// <summary>Weekly account-wide cap of 3 clears applies across all "에픽 던전" entries regardless of
    /// which character registered them, so the clear count below is taken from the raw (unfiltered)
    /// response, not the registration_flag=="true" pool.</summary>
    private const string EpicDungeonContentName = "에픽 던전";

    public static ScheduledContentPool BuildPool(SchedulerCharacterStateResponse response)
        => new()
        {
            DailyContents = NormalizeContent(response.DailyContents),
            WeeklyContents = NormalizeContent(response.WeeklyContents),
            BossContents = NormalizeBoss(response.BossContents),
            WeeklyBossClearCount = response.WeeklyBossClearCount,
            WeeklyBossClearLimitCount = response.WeeklyBossClearLimitCount,
            EpicDungeonClearedCount = response.WeeklyContents
                .Count(i => i.ContentName == EpicDungeonContentName && CompletionEvaluator.IsContentComplete(i)),
        };

    private static List<ScheduledContentItem> NormalizeContent(IEnumerable<DailyWeeklyContentItem> items)
        => items
            .Where(i => i.RegistrationFlag == RegisteredTrue)
            .Select(i => new ScheduledContentItem(i.ContentName, i.Type, i.NowCount, i.MaxCount, i.QuestState, CompletionEvaluator.IsContentComplete(i)))
            .ToList();

    private static List<ScheduledBossItem> NormalizeBoss(IEnumerable<BossContentItem> items)
        => items
            .Where(b => b.RegistrationFlag == RegisteredTrue)
            .Select(b => new ScheduledBossItem(b.ContentName, b.Difficulty, b.Cycle, CompletionEvaluator.IsBossComplete(b)))
            .ToList();
}
