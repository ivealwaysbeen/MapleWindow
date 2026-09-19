namespace MapleWindow.Core.Notifications;

public interface IOnceDailyStateStore
{
    bool WasShownToday(string ocid, string contentName, DateTime now);

    void MarkShown(string ocid, string contentName, DateTime now);
}
