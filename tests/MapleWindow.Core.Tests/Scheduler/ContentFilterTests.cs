using MapleWindow.Core.Nexon.Models;
using MapleWindow.Core.Scheduler;

namespace MapleWindow.Core.Tests.Scheduler;

public class ContentFilterTests
{
    [Fact]
    public void BuildPool_RegistrationFlagFalse_ExcludedFromAllThreeCategories()
    {
        var response = new SchedulerCharacterStateResponse
        {
            DailyContents = [new DailyWeeklyContentItem { ContentName = "d", Type = "quest", RegistrationFlag = "false", QuestState = "0" }],
            WeeklyContents = [new DailyWeeklyContentItem { ContentName = "w", Type = "contents", RegistrationFlag = "false" }],
            BossContents = [new BossContentItem { ContentName = "b", Cycle = "bossWeekly", RegistrationFlag = "false", CompleteFlag = "false" }],
        };

        var pool = ContentFilter.BuildPool(response);

        Assert.Empty(pool.DailyContents);
        Assert.Empty(pool.WeeklyContents);
        Assert.Empty(pool.BossContents);
    }

    [Fact]
    public void BuildPool_RegistrationFlagTrue_IncludedInMatchingCategory()
    {
        var response = new SchedulerCharacterStateResponse
        {
            DailyContents = [new DailyWeeklyContentItem { ContentName = "d", Type = "quest", RegistrationFlag = "true", QuestState = "2" }],
            WeeklyContents = [new DailyWeeklyContentItem { ContentName = "w", Type = "contents", RegistrationFlag = "true", NowCount = 1, MaxCount = 3 }],
            BossContents = [new BossContentItem { ContentName = "b", Cycle = "bossWeekly", RegistrationFlag = "true", CompleteFlag = "true" }],
        };

        var pool = ContentFilter.BuildPool(response);

        var daily = Assert.Single(pool.DailyContents);
        Assert.Equal("d", daily.ContentName);
        Assert.True(daily.IsComplete);   // quest_state == "2"

        var weekly = Assert.Single(pool.WeeklyContents);
        Assert.Equal("w", weekly.ContentName);
        Assert.False(weekly.IsComplete); // 1 < 3

        var boss = Assert.Single(pool.BossContents);
        Assert.Equal("b", boss.ContentName);
        Assert.True(boss.IsComplete);
    }

    [Fact]
    public void BuildPool_PassesThroughWeeklyBossClearCounts()
    {
        var response = new SchedulerCharacterStateResponse { WeeklyBossClearCount = 3, WeeklyBossClearLimitCount = 14 };

        var pool = ContentFilter.BuildPool(response);

        Assert.Equal(3, pool.WeeklyBossClearCount);
        Assert.Equal(14, pool.WeeklyBossClearLimitCount);
    }

    [Fact]
    public void BuildPool_MixedRegistrationFlags_OnlyTrueSurvive()
    {
        var response = new SchedulerCharacterStateResponse
        {
            DailyContents =
            [
                new DailyWeeklyContentItem { ContentName = "registered", Type = "quest", RegistrationFlag = "true", QuestState = "0" },
                new DailyWeeklyContentItem { ContentName = "not-registered", Type = "quest", RegistrationFlag = "false", QuestState = "0" },
            ],
        };

        var pool = ContentFilter.BuildPool(response);

        var item = Assert.Single(pool.DailyContents);
        Assert.Equal("registered", item.ContentName);
    }
}
