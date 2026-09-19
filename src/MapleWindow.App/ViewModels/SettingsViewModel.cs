using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MapleWindow.Core.Config;
using MapleWindow.Core.Notifications;
using MapleWindow.Core.Scheduler;

namespace MapleWindow.App.ViewModels;

public sealed partial class NotificationPrefItem : ObservableObject
{
    public string ContentName { get; }

    [ObservableProperty]
    private bool _muted;

    [ObservableProperty]
    private bool _onceDailyOnly;

    public NotificationPrefItem(string contentName, bool muted, bool onceDailyOnly)
    {
        ContentName = contentName;
        _muted = muted;
        _onceDailyOnly = onceDailyOnly;
    }
}

public sealed class WeaponMotionOption
{
    public string Code { get; }
    public string Label { get; }

    public WeaponMotionOption(string code, string label)
    {
        Code = code;
        Label = label;
    }
}

/// <summary>Lists content currently registered (registration_flag=="true") in the latest poll, letting the user mute or "하루 1회만 보기" each one. Changes save immediately — no explicit Save button.</summary>
public partial class SettingsViewModel : ObservableObject, IDisposable
{
    private const int MinSpeakIntervalSeconds = 5;

    private readonly INotificationPreferenceStore _preferences;
    private readonly SchedulerPollingService _polling;
    private readonly IConfigStore _configStore;
    private readonly SpeakCycleService _speakCycle;
    private readonly CharacterAppearanceService _appearanceService;
    private readonly Dispatcher _dispatcher;

    public ObservableCollection<NotificationPrefItem> Items { get; } = [];

    public IReadOnlyList<WeaponMotionOption> WeaponMotionOptions { get; } =
    [
        new("W00", "기본 모션 (W00)"),
        new("W01", "한손 모션 (W01)"),
        new("W02", "두손 모션 (W02)"),
        new("W03", "건 모션 (W03)"),
        new("W04", "무기 제외 (W04)"),
    ];

    [ObservableProperty]
    private int _speakIntervalSeconds;

    [ObservableProperty]
    private string _weaponMotion;

    public SettingsViewModel(
        INotificationPreferenceStore preferences,
        SchedulerPollingService polling,
        IConfigStore configStore,
        SpeakCycleService speakCycle,
        CharacterAppearanceService appearanceService,
        Dispatcher dispatcher)
    {
        _preferences = preferences;
        _polling = polling;
        _configStore = configStore;
        _speakCycle = speakCycle;
        _appearanceService = appearanceService;
        _dispatcher = dispatcher;
        _speakIntervalSeconds = configStore.Current?.SpeakIntervalSeconds ?? MinSpeakIntervalSeconds;
        _weaponMotion = configStore.Current?.WeaponMotion ?? "W00";
        LoadItems();

        // Re-poll (character switch, or just the next periodic tick) can land on a threadpool thread —
        // hop back to the UI thread before touching the ObservableCollection.
        _polling.PoolUpdated += OnPoolUpdated;
    }

    private void OnPoolUpdated(object? sender, ScheduledContentPool pool) => _dispatcher.BeginInvoke(LoadItems);

    public void Dispose() => _polling.PoolUpdated -= OnPoolUpdated;

    /// <summary>Applies and persists immediately (no explicit Save button, matching the mute/once-daily toggles above), clamped so a live "지금 켜져있는 프로그램" test can shorten the cycle without risking a zero/negative timer period.</summary>
    partial void OnSpeakIntervalSecondsChanged(int value)
    {
        var clamped = Math.Max(value, MinSpeakIntervalSeconds);
        if (clamped != value)
        {
            SpeakIntervalSeconds = clamped;
            return; // re-entrant call below will persist the clamped value
        }

        var config = _configStore.Current;
        if (config is null) return;

        config.SpeakIntervalSeconds = clamped;
        _configStore.Save(config);
        _speakCycle.UpdateInterval(TimeSpan.FromSeconds(clamped));
    }

    /// <summary>Applies and persists immediately, then pushes the new pose to the already-fetched character_image URL so the overlay updates without waiting for the next poll.</summary>
    partial void OnWeaponMotionChanged(string value)
    {
        var config = _configStore.Current;
        if (config is null) return;

        config.WeaponMotion = value;
        _configStore.Save(config);
        _appearanceService.UpdateWeaponMotion(value);
    }

    [RelayCommand]
    private void Refresh() => LoadItems();

    private void LoadItems()
    {
        var ocid = _configStore.Current?.Ocid ?? "";
        var pool = _polling.CurrentPool;
        var names = pool.DailyContents.Select(i => i.ContentName)
            .Concat(pool.WeeklyContents.Select(i => i.ContentName))
            .Concat(pool.BossContents.Select(b => b.ContentName))
            .Distinct()
            .OrderBy(name => name, StringComparer.CurrentCulture);

        Items.Clear();
        foreach (var name in names)
        {
            var pref = _preferences.Get(ocid, name);
            var item = new NotificationPrefItem(name, pref.Muted, pref.OnceDailyOnly);
            item.PropertyChanged += (_, _) => _preferences.Set(ocid, name, new NotificationPreference(item.Muted, item.OnceDailyOnly));
            Items.Add(item);
        }
    }
}
