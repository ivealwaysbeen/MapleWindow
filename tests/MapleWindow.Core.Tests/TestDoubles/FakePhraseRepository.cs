using MapleWindow.Core.Phrases;

namespace MapleWindow.Core.Tests.TestDoubles;

internal sealed class FakePhraseRepository : IPhraseRepository
{
    private readonly Dictionary<string, string> _phrases = new();

    public int ReloadCallCount { get; private set; }

    public void Set(string templateKey, string phrase) => _phrases[templateKey] = phrase;

    public void Reload() => ReloadCallCount++;

    public string? GetRandomPhrase(string templateKey) => _phrases.TryGetValue(templateKey, out var phrase) ? phrase : null;
}
