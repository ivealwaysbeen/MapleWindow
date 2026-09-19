using System.Diagnostics;

namespace MapleWindow.Core.Scheduler.PhraseRules;

/// <summary>
/// Every incomplete registered weekly_contents item gets its own message, worded by how much of the week (Thu reset) is left.
/// Exception: "에픽 던전" is capped at 3 clears account-wide (shared across all characters, see
/// ContentFilter.EpicDungeonClearedCount) — once that cap is reached, remaining incomplete 에픽 던전 entries
/// stay silent, since they can't be cleared this week regardless.
/// </summary>
public sealed class WeeklyContentRule : IPhraseRule
{
    private const string EpicDungeonContentName = "에픽 던전";
    private const long EpicDungeonWeeklyClearLimit = 3;

    public IEnumerable<PhraseMessage> Evaluate(ScheduledContentPool pool, DateTime now)
    {
        var key = DayBucketKey(now.DayOfWeek);
        var epicDungeonCapped = pool.EpicDungeonClearedCount >= EpicDungeonWeeklyClearLimit;

        foreach (var item in pool.WeeklyContents.Where(i => !i.IsComplete))
        {
            if (item.ContentName == EpicDungeonContentName && epicDungeonCapped) continue;

            yield return new PhraseMessage(
                [item.ContentName],
                key,
                new Dictionary<string, string> { ["content_name"] = item.ContentName });
        }
    }

    private static string DayBucketKey(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday or DayOfWeek.Tuesday or DayOfWeek.Wednesday => "weekly.content.monToWed",
        DayOfWeek.Thursday or DayOfWeek.Friday => "weekly.content.thuFri",
        DayOfWeek.Saturday or DayOfWeek.Sunday => "weekly.content.satSun",
        _ => throw new UnreachableException(),
    };
}
