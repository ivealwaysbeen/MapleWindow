namespace MapleWindow.Core.Notifications;

public interface INotificationPreferenceStore
{
    /// <summary>Returns NotificationPreference.Default (not muted, not once-daily) when this character has no saved preference for the content.</summary>
    NotificationPreference Get(string ocid, string contentName);

    void Set(string ocid, string contentName, NotificationPreference preference);

    /// <summary>For the settings UI: everything with an explicitly saved preference for this character.</summary>
    IReadOnlyDictionary<string, NotificationPreference> GetAll(string ocid);
}
