using System.Diagnostics;

namespace MapleWindow.Core.Scheduler.PhraseRules;

/// <summary>Every incomplete registered weekly_contents item gets its own message, worded by how much of the week (Thu reset) is left.</summary>
public sealed class WeeklyContentRule : IPhraseRule
{
    public IEnumerable<PhraseMessage> Evaluate(ScheduledContentPool pool, DateTime now)
    {
        var key = DayBucketKey(now.DayOfWeek);

        foreach (var item in pool.WeeklyContents.Where(i => !i.IsComplete))
        {
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
