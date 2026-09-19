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
    private Timer? _dailyTimer;

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

    /// <summary>Forces one extra poll shortly after the daily content reset (00:01 local), independent of
    /// the regular interval poll from <see cref="Start"/> — so today's freshly-reset content shows up
    /// without waiting for the next periodic tick.</summary>
    public void StartDailyRefresh() => ScheduleNextDailyRefresh();

    private void ScheduleNextDailyRefresh()
    {
        var delay = DelayUntilNextDailyRefresh(DateTime.Now);
        _dailyTimer = new Timer(async _ =>
        {
            await PollOnceAsync().ConfigureAwait(false);
            ScheduleNextDailyRefresh();
        }, null, delay, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Pure so it can be unit tested without a real timer: the wait until the next local 00:01,
    /// recomputed from wall-clock time on every fire (rather than a fixed 24h period) so it can't drift and
    /// self-corrects across DST changes.</summary>
    public static TimeSpan DelayUntilNextDailyRefresh(DateTime now)
    {
        var todayAt0001 = now.Date.AddMinutes(1);
        var nextRun = now < todayAt0001 ? todayAt0001 : todayAt0001.AddDays(1);
        return nextRun - now;
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

    public void Dispose()
    {
        _timer?.Dispose();
        _dailyTimer?.Dispose();
    }
}
