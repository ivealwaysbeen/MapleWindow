using System.Text.Json;

namespace MapleWindow.Core.Notifications;

/// <summary>Tracks the last calendar date (KST-agnostic — uses whatever DateTime the caller passes) a "하루 1회만 보기" content item was shown.</summary>
public sealed class OnceDailyStateStore : IOnceDailyStateStore
{
    private const string DateFormat = "yyyy-MM-dd";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private readonly string _path;
    private Dictionary<string, string> _lastShownDates = new();

    public OnceDailyStateStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleWindow", "notification-state.json");
        Load();
    }

    public bool WasShownToday(string contentName, DateTime now)
        => _lastShownDates.TryGetValue(contentName, out var lastShown) && lastShown == now.ToString(DateFormat);

    public void MarkShown(string contentName, DateTime now)
    {
        _lastShownDates[contentName] = now.ToString(DateFormat);
        Save();
    }

    private void Load()
    {
        if (!File.Exists(_path))
        {
            _lastShownDates = new();
            return;
        }

        var json = File.ReadAllText(_path);
        _lastShownDates = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_path, JsonSerializer.Serialize(_lastShownDates, JsonOptions));
    }
}
