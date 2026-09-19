using MapleWindow.Core.Nexon;
using MapleWindow.Core.Nexon.Models;

namespace MapleWindow.Core.Tests.TestDoubles;

/// <summary>Hand-rolled test double — no mocking library in the project, and this interface is small enough not to need one.</summary>
internal sealed class FakeNexonApiClient : INexonApiClient
{
    public CharacterListResponse CharacterListResponse { get; set; } = new();
    public CharacterBasicResponse CharacterBasicResponse { get; set; } = new();
    public SchedulerCharacterStateResponse SchedulerResponse { get; set; } = new();
    public Exception? SchedulerException { get; set; }
    public int GetSchedulerStateCallCount { get; private set; }
    public int GetCharacterBasicCallCount { get; private set; }

    public Task<CharacterListResponse> GetCharacterListAsync(string apiKey, CancellationToken ct = default)
        => Task.FromResult(CharacterListResponse);

    public Task<CharacterBasicResponse> GetCharacterBasicAsync(string apiKey, string ocid, string? date = null, CancellationToken ct = default)
    {
        GetCharacterBasicCallCount++;
        return Task.FromResult(CharacterBasicResponse);
    }

    public Task<SchedulerCharacterStateResponse> GetSchedulerStateAsync(string apiKey, string ocid, string? date = null, CancellationToken ct = default)
    {
        GetSchedulerStateCallCount++;
        return SchedulerException is not null
            ? Task.FromException<SchedulerCharacterStateResponse>(SchedulerException)
            : Task.FromResult(SchedulerResponse);
    }
}
