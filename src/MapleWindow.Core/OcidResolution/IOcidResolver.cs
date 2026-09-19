namespace MapleWindow.Core.OcidResolution;

/// <summary>
/// Nexon's docs warn that a character's ocid "can change due to game content changes" — so instead of
/// trusting a cached ocid forever, we re-resolve it from character_name + world_name via /character/list
/// (see MapleWindow.Core.Nexon.INexonApiClient.GetCharacterListAsync).
/// </summary>
public interface IOcidResolver
{
    Task<OcidResolutionResult> ResolveAsync(string apiKey, string characterName, string worldName, CancellationToken ct = default);
}
