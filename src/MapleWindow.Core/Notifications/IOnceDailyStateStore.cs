namespace MapleWindow.Core.Notifications;

public interface IOnceDailyStateStore
{
    bool WasShownToday(string contentName, DateTime now);

    void MarkShown(string contentName, DateTime now);
}
