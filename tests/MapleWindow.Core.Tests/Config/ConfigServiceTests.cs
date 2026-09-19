using System.Text.Json.Nodes;
using MapleWindow.Core.Config;

namespace MapleWindow.Core.Tests.Config;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"maplewindow-test-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(_tempPath)) File.Delete(_tempPath);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields_ApiKeyEncryptedAtRest()
    {
        var service = new ConfigService(new DpapiProtector(), _tempPath);
        var config = new AppConfig
        {
            ApiKey = "test_094a02c1ed26b3667342a47ae783cb3a3f449ff287094f1591d8c3a90da372f1efe8d04e6d233bd35cf2fabdeb93fb0d",
            Ocid = "ocid-abc",
            CharacterName = "용사one",
            WorldName = "스카니아",
            PollIntervalSeconds = 300,
            SpeakIntervalSeconds = 1800,
            WeaponMotion = "W02",
            CharacterScale = 5,
        };

        service.Save(config);

        var rawJson = File.ReadAllText(_tempPath);
        Assert.DoesNotContain(config.ApiKey, rawJson);

        var loaded = service.Load();

        Assert.NotNull(loaded);
        Assert.Equal(config.ApiKey, loaded!.ApiKey);
        Assert.Equal(config.Ocid, loaded.Ocid);
        Assert.Equal(config.CharacterName, loaded.CharacterName);
        Assert.Equal(config.WorldName, loaded.WorldName);
        Assert.Equal(config.PollIntervalSeconds, loaded.PollIntervalSeconds);
        Assert.Equal(config.SpeakIntervalSeconds, loaded.SpeakIntervalSeconds);
        Assert.Equal(config.WeaponMotion, loaded.WeaponMotion);
        Assert.Equal(config.CharacterScale, loaded.CharacterScale);
    }

    [Fact]
    public void Load_NoFileExists_ReturnsNull()
    {
        var service = new ConfigService(new DpapiProtector(), _tempPath);

        Assert.Null(service.Load());
    }

    [Fact]
    public void Load_LegacyConfigWithoutWeaponMotion_DefaultsToW00()
    {
        var service = new ConfigService(new DpapiProtector(), _tempPath);
        service.Save(new AppConfig { ApiKey = "k", Ocid = "o", CharacterName = "n", WorldName = "w" });
        var json = JsonNode.Parse(File.ReadAllText(_tempPath))!.AsObject();
        json.Remove("WeaponMotion");
        File.WriteAllText(_tempPath, json.ToJsonString());

        var loaded = service.Load();

        Assert.Equal("W00", loaded!.WeaponMotion);
    }

    [Fact]
    public void Load_LegacyConfigWithoutCharacterScale_DefaultsTo3()
    {
        var service = new ConfigService(new DpapiProtector(), _tempPath);
        service.Save(new AppConfig { ApiKey = "k", Ocid = "o", CharacterName = "n", WorldName = "w" });
        var json = JsonNode.Parse(File.ReadAllText(_tempPath))!.AsObject();
        json.Remove("CharacterScale");
        File.WriteAllText(_tempPath, json.ToJsonString());

        var loaded = service.Load();

        Assert.Equal(3, loaded!.CharacterScale);
    }

    [Fact]
    public void Current_ReflectsMostRecentLoadOrSave()
    {
        var service = new ConfigService(new DpapiProtector(), _tempPath);
        Assert.Null(service.Current);

        service.Save(new AppConfig { ApiKey = "k", Ocid = "o", CharacterName = "n", WorldName = "w" });

        Assert.Equal("o", service.Current?.Ocid);
    }
}
