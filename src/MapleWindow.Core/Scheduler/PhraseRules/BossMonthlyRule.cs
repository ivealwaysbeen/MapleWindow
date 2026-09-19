namespace MapleWindow.Core.Scheduler.PhraseRules;

/// <summary>
/// Every incomplete registered monthly (cycle=="bossMonthly") boss gets its own message, worded by how
/// much of the month is left. Boundary (per plan's noted assumption): day 1-20 inclusive = "early", 21+ = "late".
/// </summary>
public sealed class BossMonthlyRule : IPhraseRule
{
    private const string Cycle = "bossMonthly";
    private const int EarlyLateBoundaryDay = 20;

    public IEnumerable<PhraseMessage> Evaluate(ScheduledContentPool pool, DateTime now)
    {
        var key = now.Day <= EarlyLateBoundaryDay ? "boss.monthly.early" : "boss.monthly.late";

        foreach (var boss in pool.BossContents.Where(b => b.Cycle == Cycle && !b.IsComplete))
        {
            yield return new PhraseMessage(
                [boss.ContentName],
                key,
                new Dictionary<string, string> { ["content_name"] = BossLabelFormatter.Format(boss.ContentName, boss.Difficulty) });
        }
    }
}
