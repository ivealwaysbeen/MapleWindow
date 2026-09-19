using MapleWindow.Core.Notifications;
using MapleWindow.Core.Phrases;
using MapleWindow.Core.Scheduler.PhraseRules;

namespace MapleWindow.Core.Scheduler;

/// <summary>
/// A phrase resolved to actual display text, ready for the speech bubble queue. EmphasisTerms holds the
/// substituted content-name/difficulty values (in template order) so the bubble can render them Bold
/// against the default Light weight.
/// </summary>
public sealed record ResolvedSpeech(IReadOnlyList<string> RelatedContentNames, string Text, IReadOnlyList<string> EmphasisTerms);

/// <summary>
/// 30-minute-default speak timer, independent of SchedulerPollingService's poll interval — reads the
/// already-cached pool (no network call), runs all 5 phrase rules in the required daily -> weekly ->
/// boss(weekly -> monthly) order, applies mute/once-daily filtering, and resolves each surviving
/// message to display text.
/// </summary>
public sealed class SpeakCycleService : IDisposable
{
    private static readonly IReadOnlyList<IPhraseRule> Rules =
    [
        new DailyQuestRule(),
        new NamedDailyContentRule(),
        new WeeklyContentRule(),
        new BossWeeklyRule(),
        new BossMonthlyRule(),
    ];

    private readonly Func<ScheduledContentPool> _currentPool;
    private readonly Func<string?> _currentOcid;
    private readonly IPhraseRepository _phrases;
    private readonly INotificationPreferenceStore _preferences;
    private readonly IOnceDailyStateStore _onceDailyState;
    private readonly Func<DateTime> _clock;
    private Timer? _timer;

    public event EventHandler<IReadOnlyList<ResolvedSpeech>>? QueueReady;

    /// <summary>Raised whenever the interval is changed live (e.g. from Settings), so a currently-displayed
    /// speech bubble/queue (owned by the overlay, not this service) can be cleared and restarted from scratch
    /// instead of finishing out a cadence computed under the old interval.</summary>
    public event EventHandler? IntervalChanged;

    /// <summary>Raised after a character switch, so a queue already built from the previous character's pool
    /// (the pool this service reads is a live pointer — see the DI registration in ServiceCollectionExtensions —
    /// so old queued lines wouldn't naturally clear on their own) gets dropped instead of playing out.</summary>
    public event EventHandler? QueueResetRequested;

    public SpeakCycleService(
        Func<ScheduledContentPool> currentPool,
        Func<string?> currentOcid,
        IPhraseRepository phrases,
        INotificationPreferenceStore preferences,
        IOnceDailyStateStore onceDailyState,
        Func<DateTime>? clock = null)
    {
        _currentPool = currentPool;
        _currentOcid = currentOcid;
        _phrases = phrases;
        _preferences = preferences;
        _onceDailyState = onceDailyState;
        _clock = clock ?? (() => DateTime.Now);
    }

    /// <summary>dueTime is zero so the first speech fires right away instead of waiting a full interval;
    /// subsequent ticks still run every `interval`.</summary>
    public void Start(TimeSpan interval)
    {
        _timer = new Timer(_ => EvaluateAndRaise(), null, TimeSpan.Zero, interval);
    }

    /// <summary>Rescheduling a running timer (rather than a Stop/Start pair) preserves it across a live settings
    /// change. IntervalChanged fires first so listeners clear the stale queue/bubble before the immediate
    /// (dueTime-zero) re-evaluation below raises the next one under the new interval.</summary>
    public void UpdateInterval(TimeSpan interval)
    {
        IntervalChanged?.Invoke(this, EventArgs.Empty);
        _timer?.Change(TimeSpan.Zero, interval);
    }

    /// <summary>Call after switching characters so a bubble/queue already built from the old character's pool
    /// doesn't keep playing out over the new one.</summary>
    public void ResetQueue() => QueueResetRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Public (not just timer-invoked) so tests and a manual "지금 말하기" trigger can call it directly.</summary>
    public IReadOnlyList<ResolvedSpeech> EvaluateAndRaise()
    {
        var now = _clock();
        var pool = _currentPool();
        var ocid = _currentOcid() ?? "";

        var messages = Rules.SelectMany(rule => rule.Evaluate(pool, now));
        var filtered = NotificationFilter.Apply(messages, ocid, _preferences, _onceDailyState, now);

        var resolved = new List<ResolvedSpeech>();
        foreach (var message in filtered)
        {
            var template = _phrases.GetRandomPhrase(message.TemplateKey);
            if (template is null) continue; // no phrase authored for this key yet -> silently skip

            resolved.Add(new ResolvedSpeech(
                message.RelatedContentNames,
                PhraseFormatter.Format(template, message.Tokens),
                message.Tokens.Values.ToList()));
        }

        if (resolved.Count > 0)
            QueueReady?.Invoke(this, resolved);

        return resolved;
    }

    public void Dispose() => _timer?.Dispose();
}
