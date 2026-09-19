using MapleWindow.Core.Notifications;

namespace MapleWindow.Core.Tests.TestDoubles;

internal sealed class FakeNotificationPreferenceStore : INotificationPreferenceStore
{
    private readonly Dictionary<string, NotificationPreference> _entries = new();

    public NotificationPreference Get(string contentName)
        => _entries.TryGetValue(contentName, out var pref) ? pref : NotificationPreference.Default;

    public void Set(string contentName, NotificationPreference preference) => _entries[contentName] = preference;

    public IReadOnlyDictionary<string, NotificationPreference> GetAll() => _entries;
}

internal sealed class FakeOnceDailyStateStore : IOnceDailyStateStore
{
    private readonly Dictionary<string, DateTime> _shownDates = new();

    public bool WasShownToday(string contentName, DateTime now)
        => _shownDates.TryGetValue(contentName, out var date) && date.Date == now.Date;

    public void MarkShown(string contentName, DateTime now) => _shownDates[contentName] = now;
}
