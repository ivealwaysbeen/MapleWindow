using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
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

    /// <summary>Display label for the group this item belongs to — bound as the grouping key for
    /// SettingsWindow's ListView, which shows one header per distinct value it encounters.</summary>
    public string GroupLabel { get; }

    [ObservableProperty]
    private bool _muted;

    [ObservableProperty]
    private bool _onceDailyOnly;

    public NotificationPrefItem(string contentName, string groupLabel, bool muted, bool onceDailyOnly)
    {
        ContentName = contentName;
        GroupLabel = groupLabel;
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

/// <summary>Lists content currently registered (registration_flag=="true") in the latest poll, letting the user mute or "하루 1회만 보기" each one. Every control here is an in-memory edit only — nothing is
/// persisted or takes effect until the "적용" button (<see cref="ApplyCommand"/>) is clicked; closing the
/// window without applying discards the edits.</summary>
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

    /// <summary>Groups Items by NotificationPrefItem.GroupLabel for SettingsWindow's ListView. Items is
    /// always rebuilt one group at a time (see LoadItems), so each group's rows stay contiguous and the
    /// view's default first-appearance group ordering matches the intended 일간→주간→보스(주간)→보스(월간) order
    /// without needing an explicit group sort.</summary>
    public ICollectionView ItemsView { get; }

    public IReadOnlyList<WeaponMotionOption> WeaponMotionOptions { get; } =
    [
        new("W00", "기본 모션 (W00)"),
        new("W01", "한손 모션 (W01)"),
        new("W02", "두손 모션 (W02)"),
        new("W03", "건 모션 (W03)"),
        new("W04", "무기 제외 (W04)"),
    ];

    public IReadOnlyList<int> CharacterScaleOptions { get; } = [1, 2, 3, 4, 5];

    [ObservableProperty]
    private int _speakIntervalSeconds;

    [ObservableProperty]
    private string _weaponMotion;

    [ObservableProperty]
    private int _characterScale;

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
        _characterScale = configStore.Current?.CharacterScale ?? 3;
        ItemsView = CollectionViewSource.GetDefaultView(Items);
        ItemsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(NotificationPrefItem.GroupLabel)));
        LoadItems();

        // Re-poll (character switch, or just the next periodic tick) can land on a threadpool thread —
        // hop back to the UI thread before touching the ObservableCollection.
        _polling.PoolUpdated += OnPoolUpdated;
    }

    private void OnPoolUpdated(object? sender, ScheduledContentPool pool) => _dispatcher.BeginInvoke(LoadItems);

    public void Dispose() => _polling.PoolUpdated -= OnPoolUpdated;

    /// <summary>Clamps live so typing can't leave the field at a zero/negative value, but does not persist —
    /// persisting/applying happens only in <see cref="Apply"/>.</summary>
    partial void OnSpeakIntervalSecondsChanged(int value)
    {
        var clamped = Math.Max(value, MinSpeakIntervalSeconds);
        if (clamped != value) SpeakIntervalSeconds = clamped; // re-entrant call is a no-op since it's already clamped
    }

    [RelayCommand]
    private void Apply()
    {
        var config = _configStore.Current;
        if (config is not null)
        {
            config.SpeakIntervalSeconds = SpeakIntervalSeconds;
            config.WeaponMotion = WeaponMotion;
            config.CharacterScale = CharacterScale;
            _configStore.Save(config);
        }

        _speakCycle.UpdateInterval(TimeSpan.FromSeconds(SpeakIntervalSeconds));
        _appearanceService.UpdateWeaponMotion(WeaponMotion);

        var ocid = config?.Ocid ?? "";
        foreach (var item in Items)
        {
            _preferences.Set(ocid, item.ContentName, new NotificationPreference(item.Muted, item.OnceDailyOnly));
        }
    }

    private void LoadItems()
    {
        var ocid = _configStore.Current?.Ocid ?? "";
        var pool = _polling.CurrentPool;

        // A background poll can rebuild Items while the window is open with unapplied edits sitting in it —
        // carry those forward instead of clobbering them with the last-applied (persisted) value. Content
        // that isn't in this snapshot (freshly appeared this poll) falls back to the persisted preference.
        var pending = Items.ToDictionary(i => i.ContentName, i => (i.Muted, i.OnceDailyOnly));

        Items.Clear();
        AddGroup(ocid, "일간 콘텐츠", pool.DailyContents.Select(i => i.ContentName), pending);
        AddGroup(ocid, "주간 콘텐츠", pool.WeeklyContents.Select(i => i.ContentName), pending);
        AddGroup(ocid, "보스 (주간)", pool.BossContents.Where(b => b.Cycle == "bossWeekly").Select(b => b.ContentName), pending);
        AddGroup(ocid, "보스 (월간)", pool.BossContents.Where(b => b.Cycle == "bossMonthly").Select(b => b.ContentName), pending);
    }

    /// <summary>Adds one group's rows to Items, contiguously — see ItemsView's remarks for why that matters
    /// for group ordering.</summary>
    private void AddGroup(
        string ocid,
        string groupLabel,
        IEnumerable<string> contentNames,
        IReadOnlyDictionary<string, (bool Muted, bool OnceDailyOnly)> pending)
    {
        foreach (var name in contentNames.Distinct().OrderBy(name => name, StringComparer.CurrentCulture))
        {
            var (muted, onceDailyOnly) = pending.TryGetValue(name, out var edit)
                ? edit
                : (_preferences.Get(ocid, name).Muted, _preferences.Get(ocid, name).OnceDailyOnly);
            Items.Add(new NotificationPrefItem(name, groupLabel, muted, onceDailyOnly));
        }
    }
}
