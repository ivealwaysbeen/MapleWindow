using MapleWindow.Core.Nexon.Models;
using MapleWindow.Core.Scheduler;

namespace MapleWindow.Core.Tests.Scheduler;

public class CompletionEvaluatorTests
{
    private static DailyWeeklyContentItem ContentsItem(long now, long max)
        => new() { ContentName = "몬스터 파크", Type = "contents", RegistrationFlag = "true", NowCount = now, MaxCount = max, QuestState = "0" };

    private static DailyWeeklyContentItem QuestItem(string questState)
        => new() { ContentName = "[일일 퀘스트] 카르시온 복구 지원", Type = "quest", RegistrationFlag = "true", NowCount = 0, MaxCount = 1, QuestState = questState };

    private static BossContentItem BossItem(string completeFlag)
        => new() { ContentName = "듄켈", Difficulty = "노멀", Cycle = "bossWeekly", RegistrationFlag = "true", CompleteFlag = completeFlag };

    [Theory]
    [InlineData(0, 5, false)]
    [InlineData(3, 5, false)]
    [InlineData(5, 5, true)]
    [InlineData(9, 5, true)]
    public void IsContentComplete_ContentsType_NormalMaxCount_UsesNowGreaterOrEqualMax(long now, long max, bool expected)
    {
        Assert.Equal(expected, CompletionEvaluator.IsContentComplete(ContentsItem(now, max)));
    }

    [Fact]
    public void IsContentComplete_ContentsType_MaxCountZero_NowCountZero_IsNotComplete()
    {
        // The buggy naive rule (now >= max) would call this complete (0 >= 0) even though nothing was done.
        Assert.False(CompletionEvaluator.IsContentComplete(ContentsItem(now: 0, max: 0)));
    }

    [Fact]
    public void IsContentComplete_ContentsType_MaxCountZero_NowCountPositive_IsComplete()
    {
        Assert.True(CompletionEvaluator.IsContentComplete(ContentsItem(now: 1, max: 0)));
    }

    [Theory]
    [InlineData("0", false)]
    [InlineData("1", false)]
    [InlineData("2", true)]
    public void IsContentComplete_QuestType_UsesQuestState(string questState, bool expected)
    {
        Assert.Equal(expected, CompletionEvaluator.IsContentComplete(QuestItem(questState)));
    }

    [Fact]
    public void IsContentComplete_QuestType_IgnoresCountsEvenIfCountsWouldSayOtherwise()
    {
        var item = QuestItem("1");
        item.NowCount = 999;
        item.MaxCount = 1;

        Assert.False(CompletionEvaluator.IsContentComplete(item));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void IsBossComplete_UsesCompleteFlag(string completeFlag, bool expected)
    {
        Assert.Equal(expected, CompletionEvaluator.IsBossComplete(BossItem(completeFlag)));
    }
}
