using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Scheduler.PhraseRules;

namespace MapleWindow.Core.Tests.Scheduler.PhraseRules;

public class DailyQuestRuleTests
{
    private static readonly DailyQuestRule Rule = new();
    private static readonly DateTime AnyDay = new(2026, 9, 21); // Monday

    private static ScheduledContentItem Quest(string name, bool isComplete)
        => new(name, "quest", 0, 1, isComplete ? "2" : "0", isComplete);

    [Fact]
    public void NoRegisteredQuests_NoMessages()
    {
        var pool = new ScheduledContentPool { DailyContents = [] };

        Assert.Empty(Rule.Evaluate(pool, AnyDay));
    }

    [Fact]
    public void AllIncomplete_OneCollapsedMessage_RelatedToAllNames()
    {
        var pool = new ScheduledContentPool { DailyContents = [Quest("퀘스트A", false), Quest("퀘스트B", false)] };

        var message = Assert.Single(Rule.Evaluate(pool, AnyDay));

        Assert.Equal("daily.quest.allUndone", message.TemplateKey);
        Assert.Equal(["퀘스트A", "퀘스트B"], message.RelatedContentNames);
    }

    [Fact]
    public void SomeIncomplete_OneMessagePerIncompleteItem()
    {
        var pool = new ScheduledContentPool { DailyContents = [Quest("퀘스트A", true), Quest("퀘스트B", false), Quest("퀘스트C", false)] };

        var messages = Rule.Evaluate(pool, AnyDay).ToList();

        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal("daily.quest.someUndone", m.TemplateKey));
        Assert.Equal(["퀘스트B"], messages[0].RelatedContentNames);
        Assert.Equal("퀘스트B", messages[0].Tokens["content_name"]);
        Assert.Equal(["퀘스트C"], messages[1].RelatedContentNames);
    }

    [Fact]
    public void AllComplete_NoMessages()
    {
        var pool = new ScheduledContentPool { DailyContents = [Quest("퀘스트A", true), Quest("퀘스트B", true)] };

        Assert.Empty(Rule.Evaluate(pool, AnyDay));
    }
}
