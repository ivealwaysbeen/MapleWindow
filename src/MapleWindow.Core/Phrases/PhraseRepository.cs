using System.Reflection;
using System.Text.Json;

namespace MapleWindow.Core.Phrases;

public sealed class PhraseRepository : IPhraseRepository
{
    private const string DefaultResourceSuffix = "Phrases.phrases.default.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    private readonly string _path;
    private readonly Random _rng = new();
    private PhraseBank _bank = new();

    public PhraseRepository(string? path = null)
    {
        _path = path ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MapleWindow", "phrases.json");

        EnsureSeeded();
        Reload();
    }

    public void Reload()
    {
        if (!File.Exists(_path))
        {
            _bank = new PhraseBank();
            return;
        }

        var json = File.ReadAllText(_path);
        _bank = JsonSerializer.Deserialize<PhraseBank>(json, JsonOptions) ?? new PhraseBank();
    }

    public string? GetRandomPhrase(string templateKey)
    {
        if (!_bank.Entries.TryGetValue(templateKey, out var candidates) || candidates.Count == 0)
            return null;

        return candidates[_rng.Next(candidates.Count)];
    }

    private void EnsureSeeded()
    {
        if (File.Exists(_path)) return;

        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith(DefaultResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded default phrases resource ending in '{DefaultResourceSuffix}' was not found.");

        using var resourceStream = assembly.GetManifestResourceStream(resourceName)!;
        using var fileStream = File.Create(_path);
        resourceStream.CopyTo(fileStream);
    }
}
