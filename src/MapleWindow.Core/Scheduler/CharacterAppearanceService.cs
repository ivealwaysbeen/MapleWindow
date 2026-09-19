using System.Text.RegularExpressions;
using MapleWindow.Core.Config;
using MapleWindow.Core.Nexon;

namespace MapleWindow.Core.Scheduler;

/// <summary>Resolves the character_image base URL (from /character/basic) that ImageCache.CharacterImageCache appends ?action=..&amp; frame params to.</summary>
public sealed partial class CharacterAppearanceService
{
    private readonly INexonApiClient _api;
    private readonly IConfigStore _configStore;

    private string? _rawCharacterImageUrl;

    public string? CharacterImageBaseUrl { get; private set; }

    public CharacterAppearanceService(INexonApiClient api, IConfigStore configStore)
    {
        _api = api;
        _configStore = configStore;
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        var config = _configStore.Current;
        if (config is null) return;

        var basic = await _api.GetCharacterBasicAsync(config.ApiKey, config.Ocid, ct: ct).ConfigureAwait(false);
        _rawCharacterImageUrl = basic.CharacterImage;
        CharacterImageBaseUrl = ApplyWeaponMotion(_rawCharacterImageUrl, config.WeaponMotion);
    }

    /// <summary>Re-applies a changed wmotion setting to the already-fetched character_image URL immediately, so the
    /// on-screen pose updates without waiting for the next poll (the API itself has no wmotion request param —
    /// only the returned static image URL's query string does).</summary>
    public void UpdateWeaponMotion(string weaponMotionCode)
    {
        if (_rawCharacterImageUrl is null) return;
        CharacterImageBaseUrl = ApplyWeaponMotion(_rawCharacterImageUrl, weaponMotionCode);
    }

    private static string ApplyWeaponMotion(string baseUrl, string weaponMotionCode)
    {
        var withoutWmotion = WeaponMotionParamPattern().Replace(baseUrl, "$1").TrimEnd('?', '&');
        var separator = withoutWmotion.Contains('?') ? '&' : '?';
        return $"{withoutWmotion}{separator}wmotion={weaponMotionCode}";
    }

    [GeneratedRegex(@"([?&])wmotion=[^&]*&?")]
    private static partial Regex WeaponMotionParamPattern();
}
