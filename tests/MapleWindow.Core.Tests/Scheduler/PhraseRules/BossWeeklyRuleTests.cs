using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Scheduler.PhraseRules;

namespace MapleWindow.Core.Tests.Scheduler.PhraseRules;

public class BossWeeklyRuleTests
{
    private static readonly BossWeeklyRule Rule = new();

    private static ScheduledBossItem Boss(string name, bool isComplete, string difficulty = "노멀")
        => new(name, difficulty, "bossWeekly", isComplete);

    [Fact]
    public void NoWeeklyBosses_NoMessages()
    {
        var pool = new ScheduledContentPool { BossContents = [] };

        Assert.Empty(Rule.Evaluate(pool, new DateTime(2026, 9, 21)));
    }

    // 2026-09-21=Mon 22=Tue 23=Wed 24=Thu 25=Fri 26=Sat 27=Sun (verified).
    [Theory]
    [InlineData(24, "boss.weekly.allFalse.thuToSat")] // Thu
    [InlineData(25, "boss.weekly.allFalse.thuToSat")] // Fri
    [InlineData(26, "boss.weekly.allFalse.thuToSat")] // Sat
    [InlineData(27, "boss.weekly.allFalse.sunToWed")] // Sun
    [InlineData(21, "boss.weekly.allFalse.sunToWed")] // Mon
    [InlineData(22, "boss.weekly.allFalse.sunToWed")] // Tue
    [InlineData(23, "boss.weekly.allFalse.sunToWed")] // Wed
    public void AllIncomplete_UsesDayOfWeekBucket(int septemberDay, string expectedKey)
    {
        var pool = new ScheduledContentPool { BossContents = [Boss("듄켈", false), Boss("루오", false)] };

        var message = Assert.Single(Rule.Evaluate(pool, new DateTime(2026, 9, septemberDay)));

        Assert.Equal(expectedKey, message.TemplateKey);
        Assert.Equal(["듄켈", "루오"], message.RelatedContentNames);
    }

    [Fact]
    public void SomeIncomplete_OneMessagePerIncompleteBoss_WithDifficulty()
    {
        var pool = new ScheduledContentPool { BossContents = [Boss("듄켈", true), Boss("루오", false, "hard")] };

        var message = Assert.Single(Rule.Evaluate(pool, new DateTime(2026, 9, 21)));

        Assert.Equal("boss.weekly.someFalse", message.TemplateKey);
        Assert.Equal(["루오"], message.RelatedContentNames);
        Assert.Equal("루오 : HARD", message.Tokens["content_name"]);
    }

    [Fact]
    public void SomeIncomplete_NoDifficulty_OmitsSeparator()
    {
        var pool = new ScheduledContentPool { BossContents = [Boss("듄켈", true), Boss("카오스 자쿰", false, "")] };

        var message = Assert.Single(Rule.Evaluate(pool, new DateTime(2026, 9, 21)));

        Assert.Equal("카오스 자쿰", message.Tokens["content_name"]);
    }

    [Fact]
    public void AllComplete_ClearCountUnderLimit_EmitsNudge()
    {
        var pool = new ScheduledContentPool
        {
            BossContents = [Boss("듄켈", true), Boss("루오", true)],
            WeeklyBossClearCount = 2,
            WeeklyBossClearLimitCount = 14,
        };

        var message = Assert.Single(Rule.Evaluate(pool, new DateTime(2026, 9, 21)));

        Assert.Equal("boss.weekly.allTrueUnderLimit", message.TemplateKey);
        Assert.Equal(["듄켈", "루오"], message.RelatedContentNames);
    }

    [Fact]
    public void AllComplete_ClearCountAtLimit_Silent()
    {
        var pool = new ScheduledContentPool
        {
            BossContents = [Boss("듄켈", true), Boss("루오", true)],
            WeeklyBossClearCount = 14,
            WeeklyBossClearLimitCount = 14,
        };

        Assert.Empty(Rule.Evaluate(pool, new DateTime(2026, 9, 21)));
    }
}
