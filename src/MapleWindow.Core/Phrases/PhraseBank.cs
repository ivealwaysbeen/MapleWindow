namespace MapleWindow.Core.Phrases;

/// <summary>Deserialized shape of phrases.json — a rule key (e.g. "daily.quest.allUndone") to candidate phrase templates.</summary>
public sealed class PhraseBank
{
    public Dictionary<string, List<string>> Entries { get; set; } = new();
}
