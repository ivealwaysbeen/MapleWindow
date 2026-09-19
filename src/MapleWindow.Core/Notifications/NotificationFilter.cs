using MapleWindow.Core.Scheduler;

namespace MapleWindow.Core.Notifications;

/// <summary>
/// Applies mute + "하루 1회만 보기" preferences to the messages a poll cycle's phrase rules produced.
///   - A message is dropped if every one of its RelatedContentNames is individually muted (a single-item
///     message is dropped iff that one item is muted; a group message needs the whole group muted).
///   - Once-daily-only only applies to single-item messages (a group message isn't "about" one settings-list
///     entry, so there's no single last-shown-date to check) — see the plan's noted assumption.
/// </summary>
public static class NotificationFilter
{
    public static IReadOnlyList<PhraseMessage> Apply(
        IEnumerable<PhraseMessage> messages,
        string ocid,
        INotificationPreferenceStore preferences,
        IOnceDailyStateStore onceDailyState,
        DateTime now)
    {
        var result = new List<PhraseMessage>();

        foreach (var message in messages)
        {
            if (message.RelatedContentNames.Count == 0) continue;

            var allMuted = message.RelatedContentNames.All(name => preferences.Get(ocid, name).Muted);
            if (allMuted) continue;

            if (message.RelatedContentNames.Count == 1)
            {
                var contentName = message.RelatedContentNames[0];
                if (preferences.Get(ocid, contentName).OnceDailyOnly)
                {
                    if (onceDailyState.WasShownToday(ocid, contentName, now)) continue;
                    onceDailyState.MarkShown(ocid, contentName, now);
                }
            }

            result.Add(message);
        }

        return result;
    }
}
