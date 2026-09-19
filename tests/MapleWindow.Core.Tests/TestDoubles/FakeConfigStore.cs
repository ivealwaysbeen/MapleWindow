using MapleWindow.Core.Config;

namespace MapleWindow.Core.Tests.TestDoubles;

internal sealed class FakeConfigStore : IConfigStore
{
    public AppConfig? Current { get; private set; }

    public FakeConfigStore(AppConfig? initial = null) => Current = initial;

    public AppConfig? Load() => Current;

    public void Save(AppConfig config) => Current = config;
}
