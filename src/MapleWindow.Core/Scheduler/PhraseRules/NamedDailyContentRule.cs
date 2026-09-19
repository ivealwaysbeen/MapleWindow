namespace MapleWindow.Core.Scheduler.PhraseRules;

/// <summary>
/// Daily "contents"-type items handled by exact content_name, each with its own one-off condition
/// (the generic completion check isn't what triggers these — e.g. 몬스터 파크 only nags when now_count==0,
/// not for any progress short of max_count). Add a new named-content rule here as more are defined;
/// any daily contents item not listed here currently has no phrase and stays silent.
/// </summary>
public sealed class NamedDailyContentRule : IPhraseRule
{
    private sealed record NamedRule(string ContentName, Func<ScheduledContentItem, bool> ShouldSpeak, string TemplateKey);

    private static readonly IReadOnlyList<NamedRule> Rules =
    [
        new("몬스터 파크", item => item.NowCount == 0, "daily.monsterPark.notStarted"),
    ];

    public IEnumerable<PhraseMessage> Evaluate(ScheduledContentPool pool, DateTime now)
    {
        foreach (var rule in Rules)
        {
            var item = pool.DailyContents.FirstOrDefault(i => i.ContentName == rule.ContentName);
            if (item is null || !rule.ShouldSpeak(item)) continue;

            yield return new PhraseMessage(
                [item.ContentName],
                rule.TemplateKey,
                new Dictionary<string, string> { ["content_name"] = item.ContentName });
        }
    }
}
