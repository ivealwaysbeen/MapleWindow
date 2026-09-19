using System.Text.Json;

namespace MapleWindow.Core.Notifications;

/// <summary>Mute / "하루 1회만 보기" preferences, keyed per character (ocid). Content names (boss names, "몬스터파크",
/// story daily quests, ...) are shared across characters, so a flat contentName-only key would mean muting a
/// boss on one character silently muted it on every other character sharing that same content name.</summary>
public sealed class NotificationPreferenceStore : INotificationPreferenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private static readonly IReadOnlyDictionary<string, NotificationPreference> EmptyPreferences = new Dictionary<string, NotificationPreference>();

    private readonly string _path;
    private Dictionary<string, Dictionary<string, NotificationPreference>> _entries = new();

    public NotificationPreferenceStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleWindow", "notification-prefs.json");
        Load();
    }

    public NotificationPreference Get(string ocid, string contentName)
        => _entries.TryGetValue(ocid, out var perCharacter) && perCharacter.TryGetValue(contentName, out var preference)
            ? preference
            : NotificationPreference.Default;

    public void Set(string ocid, string contentName, NotificationPreference preference)
    {
        if (!_entries.TryGetValue(ocid, out var perCharacter))
        {
            perCharacter = new();
            _entries[ocid] = perCharacter;
        }

        perCharacter[contentName] = preference;
        Save();
    }

    public IReadOnlyDictionary<string, NotificationPreference> GetAll(string ocid)
        => _entries.TryGetValue(ocid, out var perCharacter) ? perCharacter : EmptyPreferences;

    private void Load()
    {
        if (!File.Exists(_path))
        {
            _entries = new();
            return;
        }

        var json = File.ReadAllText(_path);
        var persisted = JsonSerializer.Deserialize<PersistedPrefs>(json, JsonOptions);
        _entries = persisted?.Entries ?? new();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_path, JsonSerializer.Serialize(new PersistedPrefs { Entries = _entries }, JsonOptions));
    }

    private sealed class PersistedPrefs
    {
        public Dictionary<string, Dictionary<string, NotificationPreference>> Entries { get; set; } = new();
    }
}
