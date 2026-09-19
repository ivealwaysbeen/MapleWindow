namespace MapleWindow.Core.Config;

/// <summary>On-disk shape of config.json — the API key is stored encrypted, never in plaintext.</summary>
internal sealed class PersistedConfig
{
    public string EncryptedApiKey { get; set; } = "";
    public string Ocid { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string WorldName { get; set; } = "";
    public int PollIntervalSeconds { get; set; } = 300;
    public int SpeakIntervalSeconds { get; set; } = 1800;
    public string WeaponMotion { get; set; } = "W00";
    public int CharacterScale { get; set; } = 3;
}
