using MapleWindow.Core.Config;
using MapleWindow.Core.Nexon.Models;
using MapleWindow.Core.Scheduler;
using MapleWindow.Core.Tests.TestDoubles;

namespace MapleWindow.Core.Tests.Scheduler;

public class CharacterAppearanceServiceTests
{
    [Fact]
    public async Task RefreshAsync_NoConfig_DoesNothing()
    {
        var service = new CharacterAppearanceService(new FakeNexonApiClient(), new FakeConfigStore(initial: null));

        await service.RefreshAsync();

        Assert.Null(service.CharacterImageBaseUrl);
    }

    [Fact]
    public async Task RefreshAsync_Success_AppliesConfiguredWeaponMotion()
    {
        var api = new FakeNexonApiClient
        {
            CharacterBasicResponse = new CharacterBasicResponse { CharacterImage = "https://open.api.nexon.com/static/maplestory/character/look/ABC" },
        };
        var config = new FakeConfigStore(new AppConfig { ApiKey = "key", Ocid = "ocid-1", CharacterName = "용사one", WorldName = "스카니아", WeaponMotion = "W00" });
        var service = new CharacterAppearanceService(api, config);

        await service.RefreshAsync();

        Assert.Equal("https://open.api.nexon.com/static/maplestory/character/look/ABC?wmotion=W00", service.CharacterImageBaseUrl);
    }

    [Fact]
    public async Task RefreshAsync_ApiUrlAlreadyHasWmotion_ConfiguredValueReplacesIt()
    {
        var api = new FakeNexonApiClient
        {
            CharacterBasicResponse = new CharacterBasicResponse { CharacterImage = "https://open.api.nexon.com/static/maplestory/character/look/ABC?wmotion=W00" },
        };
        var config = new FakeConfigStore(new AppConfig { ApiKey = "key", Ocid = "ocid-1", CharacterName = "용사one", WorldName = "스카니아", WeaponMotion = "W02" });
        var service = new CharacterAppearanceService(api, config);

        await service.RefreshAsync();

        Assert.Equal("https://open.api.nexon.com/static/maplestory/character/look/ABC?wmotion=W02", service.CharacterImageBaseUrl);
    }

    [Fact]
    public async Task RefreshAsync_ApiUrlHasOtherParamsAndWmotion_PreservesOtherParams()
    {
        var api = new FakeNexonApiClient
        {
            CharacterBasicResponse = new CharacterBasicResponse { CharacterImage = "https://open.api.nexon.com/static/maplestory/character/look/ABC?other=1&wmotion=W00" },
        };
        var config = new FakeConfigStore(new AppConfig { ApiKey = "key", Ocid = "ocid-1", CharacterName = "용사one", WorldName = "스카니아", WeaponMotion = "W03" });
        var service = new CharacterAppearanceService(api, config);

        await service.RefreshAsync();

        Assert.Equal("https://open.api.nexon.com/static/maplestory/character/look/ABC?other=1&wmotion=W03", service.CharacterImageBaseUrl);
    }

    [Fact]
    public async Task UpdateWeaponMotion_AfterRefresh_UpdatesBaseUrlWithoutRefetching()
    {
        var api = new FakeNexonApiClient
        {
            CharacterBasicResponse = new CharacterBasicResponse { CharacterImage = "https://open.api.nexon.com/static/maplestory/character/look/ABC" },
        };
        var config = new FakeConfigStore(new AppConfig { ApiKey = "key", Ocid = "ocid-1", CharacterName = "용사one", WorldName = "스카니아", WeaponMotion = "W00" });
        var service = new CharacterAppearanceService(api, config);
        await service.RefreshAsync();

        service.UpdateWeaponMotion("W04");

        Assert.Equal("https://open.api.nexon.com/static/maplestory/character/look/ABC?wmotion=W04", service.CharacterImageBaseUrl);
        Assert.Equal(1, api.GetCharacterBasicCallCount);
    }

    [Fact]
    public void UpdateWeaponMotion_BeforeAnyRefresh_DoesNothing()
    {
        var service = new CharacterAppearanceService(new FakeNexonApiClient(), new FakeConfigStore(initial: null));

        service.UpdateWeaponMotion("W02");

        Assert.Null(service.CharacterImageBaseUrl);
    }
}
