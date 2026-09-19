using System.Text.Json;

namespace MapleWindow.Core.Notifications;

public sealed class NotificationPreferenceStore : INotificationPreferenceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private readonly string _path;
    private Dictionary<string, NotificationPreference> _entries = new();

    public NotificationPreferenceStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleWindow", "notification-prefs.json");
        Load();
    }

    public NotificationPreference Get(string contentName)
        => _entries.TryGetValue(contentName, out var preference) ? preference : NotificationPreference.Default;

    public void Set(string contentName, NotificationPreference preference)
    {
        _entries[contentName] = preference;
        Save();
    }

    public IReadOnlyDictionary<string, NotificationPreference> GetAll() => _entries;

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
        public Dictionary<string, NotificationPreference> Entries { get; set; } = new();
    }
}
