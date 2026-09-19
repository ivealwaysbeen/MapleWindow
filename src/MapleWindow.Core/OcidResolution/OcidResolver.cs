using MapleWindow.Core.Nexon;

namespace MapleWindow.Core.OcidResolution;

public sealed class OcidResolver : IOcidResolver
{
    private readonly INexonApiClient _api;

    public OcidResolver(INexonApiClient api)
    {
        _api = api;
    }

    public async Task<OcidResolutionResult> ResolveAsync(string apiKey, string characterName, string worldName, CancellationToken ct = default)
    {
        var list = await _api.GetCharacterListAsync(apiKey, ct).ConfigureAwait(false);

        var match = list.AccountList
            .SelectMany(account => account.CharacterList)
            .FirstOrDefault(c => c.CharacterName == characterName && c.WorldName == worldName);

        return match is null
            ? new OcidResolutionResult(OcidResolutionStatus.NotFound, null)
            : new OcidResolutionResult(OcidResolutionStatus.Found, match.Ocid);
    }
}
