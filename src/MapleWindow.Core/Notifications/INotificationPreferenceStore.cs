namespace MapleWindow.Core.Notifications;

public interface INotificationPreferenceStore
{
    /// <summary>Returns NotificationPreference.Default (not muted, not once-daily) when the content has no saved preference.</summary>
    NotificationPreference Get(string contentName);

    void Set(string contentName, NotificationPreference preference);

    /// <summary>For the settings UI: everything with an explicitly saved preference.</summary>
    IReadOnlyDictionary<string, NotificationPreference> GetAll();
}
