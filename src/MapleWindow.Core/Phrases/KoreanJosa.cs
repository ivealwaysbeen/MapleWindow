namespace MapleWindow.Core.Phrases;

/// <summary>Picks the Korean particle form matching a word's final syllable — batchim (final consonant)
/// present vs. absent (e.g. 을/를, 은/는, 이/가, 과/와). Words outside the Hangul syllable block
/// (Latin, digits, empty) fall back to the no-batchim form, since real content here is always Hangul.</summary>
public static class KoreanJosa
{
    private const char HangulBase = '가';
    private const char HangulEnd = '힣';

    public static string Attach(string word, string withBatchim, string withoutBatchim)
    {
        if (string.IsNullOrEmpty(word)) return withoutBatchim;

        var lastChar = word[^1];
        if (lastChar < HangulBase || lastChar > HangulEnd) return withoutBatchim;

        var hasBatchim = (lastChar - HangulBase) % 28 != 0;
        return hasBatchim ? withBatchim : withoutBatchim;
    }
}
