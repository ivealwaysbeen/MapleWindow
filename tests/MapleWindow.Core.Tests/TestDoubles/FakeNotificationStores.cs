using MapleWindow.Core.Notifications;

namespace MapleWindow.Core.Tests.TestDoubles;

internal sealed class FakeNotificationPreferenceStore : INotificationPreferenceStore
{
    private readonly Dictionary<(string Ocid, string ContentName), NotificationPreference> _entries = new();

    public NotificationPreference Get(string ocid, string contentName)
        => _entries.TryGetValue((ocid, contentName), out var pref) ? pref : NotificationPreference.Default;

    public void Set(string ocid, string contentName, NotificationPreference preference) => _entries[(ocid, contentName)] = preference;

    public IReadOnlyDictionary<string, NotificationPreference> GetAll(string ocid)
        => _entries.Where(e => e.Key.Ocid == ocid).ToDictionary(e => e.Key.ContentName, e => e.Value);
}

internal sealed class FakeOnceDailyStateStore : IOnceDailyStateStore
{
    private readonly Dictionary<(string Ocid, string ContentName), DateTime> _shownDates = new();

    public bool WasShownToday(string ocid, string contentName, DateTime now)
        => _shownDates.TryGetValue((ocid, contentName), out var date) && date.Date == now.Date;

    public void MarkShown(string ocid, string contentName, DateTime now) => _shownDates[(ocid, contentName)] = now;
}
