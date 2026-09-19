using System.Text.Json;
using MapleWindow.Core.Nexon.Models;

namespace MapleWindow.Core.Nexon;

public sealed class NexonApiClient : INexonApiClient
{
    private const string ApiKeyHeader = "x-nxopen-api-key";

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;

    public NexonApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress ??= new Uri("https://open.api.nexon.com");
    }

    public Task<CharacterListResponse> GetCharacterListAsync(string apiKey, CancellationToken ct = default)
        => SendAsync<CharacterListResponse>("/maplestory/v1/character/list", apiKey, ct);

    public Task<CharacterBasicResponse> GetCharacterBasicAsync(string apiKey, string ocid, string? date = null, CancellationToken ct = default)
        => SendAsync<CharacterBasicResponse>(BuildPath("/maplestory/v1/character/basic", ocid, date), apiKey, ct);

    public Task<SchedulerCharacterStateResponse> GetSchedulerStateAsync(string apiKey, string ocid, string? date = null, CancellationToken ct = default)
        => SendAsync<SchedulerCharacterStateResponse>(BuildPath("/maplestory/v1/scheduler/character-state", ocid, date), apiKey, ct);

    private static string BuildPath(string basePath, string ocid, string? date)
    {
        var path = $"{basePath}?ocid={Uri.EscapeDataString(ocid)}";
        if (!string.IsNullOrEmpty(date))
            path += $"&date={Uri.EscapeDataString(date)}";
        return path;
    }

    private async Task<T> SendAsync<T>(string path, string apiKey, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(ApiKeyHeader, apiKey);

        using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string? errorName = null;
            var message = body;
            try
            {
                var error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
                if (error is not null)
                {
                    errorName = error.Error.Name;
                    message = error.Error.Message;
                }
            }
            catch (JsonException)
            {
                // Body wasn't the documented { error: { name, message } } shape — fall back to raw body as the message.
            }
            throw new NexonApiException(response.StatusCode, errorName, message);
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new NexonApiException(response.StatusCode, null, "Empty response body");
    }
}
