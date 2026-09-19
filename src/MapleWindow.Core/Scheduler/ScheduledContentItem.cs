namespace MapleWindow.Core.Scheduler;

/// <summary>A normalized daily_contents/weekly_contents item — already filtered to registration_flag=="true", with completion pre-computed.</summary>
public sealed record ScheduledContentItem(string ContentName, string Type, long NowCount, long MaxCount, string QuestState, bool IsComplete);

/// <summary>A normalized boss_contents item — already filtered to registration_flag=="true", with completion pre-computed.</summary>
public sealed record ScheduledBossItem(string ContentName, string Difficulty, string Cycle, bool IsComplete);

/// <summary>The full set of registered (registration_flag=="true") scheduler content for one poll, grouped by category.</summary>
public sealed class ScheduledContentPool
{
    public IReadOnlyList<ScheduledContentItem> DailyContents { get; init; } = [];
    public IReadOnlyList<ScheduledContentItem> WeeklyContents { get; init; } = [];
    public IReadOnlyList<ScheduledBossItem> BossContents { get; init; } = [];
    public long WeeklyBossClearCount { get; init; }
    public long WeeklyBossClearLimitCount { get; init; }
}
