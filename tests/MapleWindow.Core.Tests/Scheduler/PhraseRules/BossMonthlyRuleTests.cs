using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Scheduler.PhraseRules;

namespace MapleWindow.Core.Tests.Scheduler.PhraseRules;

public class BossMonthlyRuleTests
{
    private static readonly BossMonthlyRule Rule = new();

    private static ScheduledBossItem Boss(string name, bool isComplete)
        => new(name, "hard", "bossMonthly", isComplete);

    [Theory]
    [InlineData(1, "boss.monthly.early")]
    [InlineData(20, "boss.monthly.early")]
    [InlineData(21, "boss.monthly.late")]
    [InlineData(31, "boss.monthly.late")]
    public void IncompleteBoss_UsesDayOfMonthBoundary(int day, string expectedKey)
    {
        var pool = new ScheduledContentPool { BossContents = [Boss("스우", false)] };

        var message = Assert.Single(Rule.Evaluate(pool, new DateTime(2026, day == 31 ? 8 : 9, day)));

        Assert.Equal(expectedKey, message.TemplateKey);
        Assert.Equal("스우 : HARD", message.Tokens["content_name"]);
    }

    [Fact]
    public void CompleteBoss_Silent()
    {
        var pool = new ScheduledContentPool { BossContents = [Boss("스우", true)] };

        Assert.Empty(Rule.Evaluate(pool, new DateTime(2026, 9, 5)));
    }

    [Fact]
    public void WeeklyCycleBoss_IgnoredByMonthlyRule()
    {
        var weeklyBoss = new ScheduledBossItem("듄켈", "노멀", "bossWeekly", false);
        var pool = new ScheduledContentPool { BossContents = [weeklyBoss] };

        Assert.Empty(Rule.Evaluate(pool, new DateTime(2026, 9, 5)));
    }
}
