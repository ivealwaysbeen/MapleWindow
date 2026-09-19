using System.Text.RegularExpressions;

namespace MapleWindow.Core.Phrases;

/// <summary>
/// Replaces {token} placeholders in a phrase template. Unknown tokens pass through literally instead of
/// throwing, so a typo in a hand-authored phrase degrades gracefully. Also supports {token:을/를} — picks
/// whichever of the two particle forms matches the batchim of the token's substituted value (see
/// <see cref="KoreanJosa"/>); an unknown token similarly passes through literally.
/// </summary>
public static partial class PhraseFormatter
{
    public static string Format(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var withJosaResolved = JosaPattern().Replace(template, match =>
            tokens.TryGetValue(match.Groups[1].Value, out var value)
                ? value + KoreanJosa.Attach(value, match.Groups[2].Value, match.Groups[3].Value)
                : match.Value);

        return TokenPattern().Replace(withJosaResolved, match =>
            tokens.TryGetValue(match.Groups[1].Value, out var value) ? value : match.Value);
    }

    [GeneratedRegex(@"\{(\w+)\}")]
    private static partial Regex TokenPattern();

    [GeneratedRegex(@"\{(\w+):([^/{}]+)/([^/{}]+)\}")]
    private static partial Regex JosaPattern();
}
