using MapleWindow.Core.Config;
using MapleWindow.Core.Nexon;
using MapleWindow.Core.Phrases;

namespace MapleWindow.Core.Scheduler;

/// <summary>5-minute-default data poll — refreshes the cached pool that SpeakCycleService reads from on its own, independent interval (no network call there).</summary>
public sealed class SchedulerPollingService : IDisposable
{
    private readonly INexonApiClient _api;
    private readonly IConfigStore _configStore;
    private readonly IPhraseRepository _phraseRepository;
    private Timer? _timer;

    public ScheduledContentPool CurrentPool { get; private set; } = new();

    public event EventHandler<ScheduledContentPool>? PoolUpdated;
    public event EventHandler<Exception>? PollFailed;

    public SchedulerPollingService(INexonApiClient api, IConfigStore configStore, IPhraseRepository phraseRepository)
    {
        _api = api;
        _configStore = configStore;
        _phraseRepository = phraseRepository;
    }

    public void Start(TimeSpan interval)
    {
        _timer = new Timer(_ => _ = PollOnceAsync(), null, interval, interval);
    }

    public async Task PollOnceAsync()
    {
        var config = _configStore.Current;
        if (config is null) return;

        try
        {
            _phraseRepository.Reload(); // pick up manual phrases.json edits, cheap
            var response = await _api.GetSchedulerStateAsync(config.ApiKey, config.Ocid).ConfigureAwait(false);
            CurrentPool = ContentFilter.BuildPool(response);
            PoolUpdated?.Invoke(this, CurrentPool);
        }
        catch (Exception ex)
        {
            // Keep the last-known-good pool and let the next tick retry.
            PollFailed?.Invoke(this, ex);
        }
    }

    public void Dispose() => _timer?.Dispose();
}
