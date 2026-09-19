using MapleWindow.Core.Nexon.Models;

namespace MapleWindow.Core.Scheduler;

/// <summary>
/// Completion rules (user-specified):
///   - type "contents": now_count >= max_count, EXCEPT when max_count == 0 (a real, observed case) where
///     "now_count >= max_count" would trivially always be true (0 >= 0) and misreport "not started" as
///     complete — so for max_count == 0, complete instead means now_count > 0.
///   - type "quest": quest_state == "2".
///   - boss_contents: complete_flag == "true".
/// </summary>
public static class CompletionEvaluator
{
    private const string CompleteQuestState = "2";
    private const string TrueFlag = "true";

    public static bool IsContentComplete(DailyWeeklyContentItem item)
        => item.Type == "quest"
            ? item.QuestState == CompleteQuestState
            : item.MaxCount > 0 ? item.NowCount >= item.MaxCount : item.NowCount > 0;

    public static bool IsBossComplete(BossContentItem item)
        => item.CompleteFlag == TrueFlag;
}
