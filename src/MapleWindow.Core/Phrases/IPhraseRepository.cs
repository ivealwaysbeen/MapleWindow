namespace MapleWindow.Core.Phrases;

public interface IPhraseRepository
{
    /// <summary>Re-reads phrases.json from disk so a manual edit is picked up without restarting the app.</summary>
    void Reload();

    /// <summary>Returns a random candidate phrase template for the given rule key, or null if the key has no entries (caller should silently skip, not crash).</summary>
    string? GetRandomPhrase(string templateKey);
}
