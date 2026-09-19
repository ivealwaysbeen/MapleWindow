namespace MapleWindow.Core.Scheduler.PhraseRules;

/// <summary>
/// Registered daily quests (type=="quest" within daily_contents):
///   - all incomplete  -> one collapsed "haven't started any" message
///   - some incomplete -> one message per incomplete quest, naming it
///   - none incomplete -> silent
/// </summary>
public sealed class DailyQuestRule : IPhraseRule
{
    private const string AllUndoneKey = "daily.quest.allUndone";
    private const string SomeUndoneKey = "daily.quest.someUndone";

    public IEnumerable<PhraseMessage> Evaluate(ScheduledContentPool pool, DateTime now)
    {
        var questItems = pool.DailyContents.Where(i => i.Type == "quest").ToList();
        if (questItems.Count == 0) yield break;

        var incomplete = questItems.Where(i => !i.IsComplete).ToList();
        if (incomplete.Count == 0) yield break;

        if (incomplete.Count == questItems.Count)
        {
            yield return new PhraseMessage(
                questItems.Select(i => i.ContentName).ToList(),
                AllUndoneKey,
                new Dictionary<string, string>());
            yield break;
        }

        foreach (var item in incomplete)
        {
            yield return new PhraseMessage(
                [item.ContentName],
                SomeUndoneKey,
                new Dictionary<string, string> { ["content_name"] = item.ContentName });
        }
    }
}
