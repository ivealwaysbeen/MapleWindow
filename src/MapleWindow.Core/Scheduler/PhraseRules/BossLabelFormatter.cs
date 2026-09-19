namespace MapleWindow.Core.Scheduler.PhraseRules;

/// <summary>
/// Combines a boss's name and difficulty (uppercased; omitted when the boss has none) into a single
/// display label, with non-breaking spaces standing in for regular ones so the speech bubble's word-wrap
/// never breaks in the middle of it.
/// </summary>
internal static class BossLabelFormatter
{
    public static string Format(string contentName, string difficulty)
    {
        var label = string.IsNullOrWhiteSpace(difficulty)
            ? contentName
            : $"{contentName} : {difficulty.ToUpperInvariant()}";
        return label.Replace(' ', ' ');
    }
}
