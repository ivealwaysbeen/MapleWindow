using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Scheduler.PhraseRules;

namespace MapleWindow.Core.Tests.Scheduler.PhraseRules;

public class NamedDailyContentRuleTests
{
    private static readonly NamedDailyContentRule Rule = new();
    private static readonly DateTime AnyDay = new(2026, 9, 21);

    private static ScheduledContentItem MonsterPark(long nowCount)
        => new("몬스터 파크", "contents", nowCount, 5, "0", nowCount >= 5);

    [Fact]
    public void MonsterPark_NowCountZero_EmitsMessage()
    {
        var pool = new ScheduledContentPool { DailyContents = [MonsterPark(0)] };

        var message = Assert.Single(Rule.Evaluate(pool, AnyDay));

        Assert.Equal("daily.monsterPark.notStarted", message.TemplateKey);
        Assert.Equal(["몬스터 파크"], message.RelatedContentNames);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void MonsterPark_NowCountPositive_Silent(long nowCount)
    {
        var pool = new ScheduledContentPool { DailyContents = [MonsterPark(nowCount)] };

        Assert.Empty(Rule.Evaluate(pool, AnyDay));
    }

    [Fact]
    public void MonsterParkNotRegistered_Silent()
    {
        var pool = new ScheduledContentPool { DailyContents = [] };

        Assert.Empty(Rule.Evaluate(pool, AnyDay));
    }

    [Fact]
    public void OtherDailyContentsItem_NotMonsterPark_NoRuleDefined_Silent()
    {
        var other = new ScheduledContentItem("오늘의 다이스", "contents", 0, 1, "0", false);
        var pool = new ScheduledContentPool { DailyContents = [other] };

        Assert.Empty(Rule.Evaluate(pool, AnyDay));
    }
}
