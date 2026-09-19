using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Scheduler.PhraseRules;

namespace MapleWindow.Core.Tests.Scheduler.PhraseRules;

public class WeeklyContentRuleTests
{
    private static readonly WeeklyContentRule Rule = new();
    private static readonly ScheduledContentItem Incomplete = new("주간 보스 레이드", "contents", 1, 5, "0", false);
    private static readonly ScheduledContentItem Complete = new("완료된 콘텐츠", "contents", 5, 5, "0", true);

    // 2026-09-21 is a Monday.
    [Theory]
    [InlineData(21, "weekly.content.monToWed")] // Mon
    [InlineData(22, "weekly.content.monToWed")] // Tue
    [InlineData(23, "weekly.content.monToWed")] // Wed
    [InlineData(24, "weekly.content.thuFri")]    // Thu
    [InlineData(25, "weekly.content.thuFri")]    // Fri
    [InlineData(26, "weekly.content.satSun")]    // Sat
    [InlineData(27, "weekly.content.satSun")]    // Sun
    public void IncompleteItem_UsesDayOfWeekBucket(int septemberDay, string expectedKey)
    {
        var pool = new ScheduledContentPool { WeeklyContents = [Incomplete] };
        var day = new DateTime(2026, 9, septemberDay);

        var message = Assert.Single(Rule.Evaluate(pool, day));

        Assert.Equal(expectedKey, message.TemplateKey);
        Assert.Equal("주간 보스 레이드", message.Tokens["content_name"]);
    }

    [Fact]
    public void CompleteItem_Silent_RegardlessOfDay()
    {
        var pool = new ScheduledContentPool { WeeklyContents = [Complete] };

        Assert.Empty(Rule.Evaluate(pool, new DateTime(2026, 9, 21)));
    }
}
