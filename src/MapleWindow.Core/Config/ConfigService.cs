using System.Text.Json;

namespace MapleWindow.Core.Config;

public sealed class ConfigService : IConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _configPath;
    private readonly IDataProtector _protector;

    public AppConfig? Current { get; private set; }

    public ConfigService(IDataProtector protector, string? configPath = null)
    {
        _protector = protector;
        _configPath = configPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleWindow", "config.json");
    }

    public AppConfig? Load()
    {
        if (!File.Exists(_configPath))
        {
            Current = null;
            return null;
        }

        var json = File.ReadAllText(_configPath);
        var persisted = JsonSerializer.Deserialize<PersistedConfig>(json, JsonOptions);
        if (persisted is null)
        {
            Current = null;
            return null;
        }

        var config = new AppConfig
        {
            ApiKey = _protector.Unprotect(persisted.EncryptedApiKey),
            Ocid = persisted.Ocid,
            CharacterName = persisted.CharacterName,
            WorldName = persisted.WorldName,
            PollIntervalSeconds = persisted.PollIntervalSeconds,
            SpeakIntervalSeconds = persisted.SpeakIntervalSeconds,
            WeaponMotion = persisted.WeaponMotion,
            CharacterScale = persisted.CharacterScale,
        };
        Current = config;
        return config;
    }

    public void Save(AppConfig config)
    {
        var directory = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var persisted = new PersistedConfig
        {
            EncryptedApiKey = _protector.Protect(config.ApiKey),
            Ocid = config.Ocid,
            CharacterName = config.CharacterName,
            WorldName = config.WorldName,
            PollIntervalSeconds = config.PollIntervalSeconds,
            SpeakIntervalSeconds = config.SpeakIntervalSeconds,
            WeaponMotion = config.WeaponMotion,
            CharacterScale = config.CharacterScale,
        };
        File.WriteAllText(_configPath, JsonSerializer.Serialize(persisted, JsonOptions));
        Current = config;
    }
}
