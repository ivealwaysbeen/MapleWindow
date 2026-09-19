namespace MapleWindow.Core.Scheduler.PhraseRules;

/// <summary>
/// Registered weekly (cycle=="bossWeekly") bosses:
///   - all incomplete  -> one collapsed message, worded by how much of the week is left
///   - some incomplete -> one message per incomplete boss, naming it + its difficulty
///   - none incomplete but weekly_boss_clear_count &lt; limit -> one "clear count not maxed" nudge
///   - none incomplete and clear count maxed -> silent
/// </summary>
public sealed class BossWeeklyRule : IPhraseRule
{
    private const string Cycle = "bossWeekly";
    private const string SomeFalseKey = "boss.weekly.someFalse";
    private const string AllTrueUnderLimitKey = "boss.weekly.allTrueUnderLimit";

    public IEnumerable<PhraseMessage> Evaluate(ScheduledContentPool pool, DateTime now)
    {
        var bosses = pool.BossContents.Where(b => b.Cycle == Cycle).ToList();
        if (bosses.Count == 0) yield break;

        var incomplete = bosses.Where(b => !b.IsComplete).ToList();
        var allNames = bosses.Select(b => b.ContentName).ToList();

        if (incomplete.Count == bosses.Count)
        {
            yield return new PhraseMessage(allNames, AllFalseDayKey(now.DayOfWeek), new Dictionary<string, string>());
            yield break;
        }

        if (incomplete.Count > 0)
        {
            foreach (var boss in incomplete)
            {
                yield return new PhraseMessage(
                    [boss.ContentName],
                    SomeFalseKey,
                    new Dictionary<string, string> { ["content_name"] = BossLabelFormatter.Format(boss.ContentName, boss.Difficulty) });
            }
            yield break;
        }

        if (pool.WeeklyBossClearCount < pool.WeeklyBossClearLimitCount)
        {
            yield return new PhraseMessage(allNames, AllTrueUnderLimitKey, new Dictionary<string, string>());
        }
    }

    private static string AllFalseDayKey(DayOfWeek day) => day switch
    {
        DayOfWeek.Thursday or DayOfWeek.Friday or DayOfWeek.Saturday => "boss.weekly.allFalse.thuToSat",
        _ => "boss.weekly.allFalse.sunToWed", // Sunday, Monday, Tuesday, Wednesday
    };
}
