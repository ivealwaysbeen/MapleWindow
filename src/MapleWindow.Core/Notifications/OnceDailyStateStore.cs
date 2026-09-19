using System.Text.Json;

namespace MapleWindow.Core.Notifications;

/// <summary>Tracks, per character (ocid), the last calendar date (KST-agnostic — uses whatever DateTime the caller
/// passes) a "하루 1회만 보기" content item was shown. Keyed by ocid because content names (boss names, "몬스터파크",
/// story daily quests, ...) are shared across characters, so a flat contentName-only key would mark content
/// "already shown today" for every other character too as soon as one character's copy of it was announced.</summary>
public sealed class OnceDailyStateStore : IOnceDailyStateStore
{
    private const string DateFormat = "yyyy-MM-dd";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private readonly string _path;
    private Dictionary<string, Dictionary<string, string>> _lastShownDates = new();

    public OnceDailyStateStore(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleWindow", "notification-state.json");
        Load();
    }

    public bool WasShownToday(string ocid, string contentName, DateTime now)
        => _lastShownDates.TryGetValue(ocid, out var perCharacter)
            && perCharacter.TryGetValue(contentName, out var lastShown)
            && lastShown == now.ToString(DateFormat);

    public void MarkShown(string ocid, string contentName, DateTime now)
    {
        if (!_lastShownDates.TryGetValue(ocid, out var perCharacter))
        {
            perCharacter = new();
            _lastShownDates[ocid] = perCharacter;
        }

        perCharacter[contentName] = now.ToString(DateFormat);
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
        _lastShownDates = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json, JsonOptions) ?? new();
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(_path, JsonSerializer.Serialize(_lastShownDates, JsonOptions));
    }
}
