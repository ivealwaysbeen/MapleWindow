using MapleWindow.Core.Nexon.Models;

namespace MapleWindow.Core.Nexon;

public interface INexonApiClient
{
    Task<CharacterListResponse> GetCharacterListAsync(string apiKey, CancellationToken ct = default);

    Task<CharacterBasicResponse> GetCharacterBasicAsync(string apiKey, string ocid, string? date = null, CancellationToken ct = default);

    Task<SchedulerCharacterStateResponse> GetSchedulerStateAsync(string apiKey, string ocid, string? date = null, CancellationToken ct = default);
}
