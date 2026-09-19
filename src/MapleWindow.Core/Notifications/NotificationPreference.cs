namespace MapleWindow.Core.Notifications;

public sealed record NotificationPreference(bool Muted, bool OnceDailyOnly)
{
    public static readonly NotificationPreference Default = new(Muted: false, OnceDailyOnly: false);
}
