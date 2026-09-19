namespace MapleWindow.Core.Nexon.Models;

public sealed class SchedulerCharacterStateResponse
{
    public string Date { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string WorldName { get; set; } = "";
    public long CharacterLevel { get; set; }
    public string CharacterClass { get; set; } = "";
    public List<DailyWeeklyContentItem> DailyContents { get; set; } = [];
    public List<DailyWeeklyContentItem> WeeklyContents { get; set; } = [];
    public List<BossContentItem> BossContents { get; set; } = [];
    public long WeeklyBossClearCount { get; set; }
    public long WeeklyBossClearLimitCount { get; set; }
}

/// <summary>Shared shape for daily_contents[] and weekly_contents[] items (type: "contents" | "quest").</summary>
public sealed class DailyWeeklyContentItem
{
    public string ContentName { get; set; } = "";
    public string Type { get; set; } = "";
    public string RegistrationFlag { get; set; } = "";
    public long NowCount { get; set; }
    public long MaxCount { get; set; }
    public string QuestState { get; set; } = "";
}

public sealed class BossContentItem
{
    public string ContentName { get; set; } = "";
    public string Difficulty { get; set; } = "";
    public string Cycle { get; set; } = "";
    public long ListOrderNo { get; set; }
    public string RegistrationFlag { get; set; } = "";
    public string CompleteFlag { get; set; } = "";
}
