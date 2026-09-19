namespace MapleWindow.Core.Config;

public interface IConfigStore
{
    /// <summary>The most recently loaded or saved config. Services should read this on every tick rather than caching it themselves, so a "캐릭터 변경" (change character) update takes effect immediately.</summary>
    AppConfig? Current { get; }

    /// <summary>Returns null when no config file exists yet (first run).</summary>
    AppConfig? Load();

    void Save(AppConfig config);
}
