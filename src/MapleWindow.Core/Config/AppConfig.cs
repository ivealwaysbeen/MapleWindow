namespace MapleWindow.Core.Config;

public sealed class AppConfig
{
    public string ApiKey { get; set; } = "";
    public string Ocid { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string WorldName { get; set; } = "";
    public int PollIntervalSeconds { get; set; } = 300;
    public int SpeakIntervalSeconds { get; set; } = 1800;

    /// <summary>Nexon character_image wmotion param (W00~W04). See CharacterAppearanceService.</summary>
    public string WeaponMotion { get; set; } = "W00";

    /// <summary>Overlay character render size, 1~5. 3 is the original/reference size.</summary>
    public int CharacterScale { get; set; } = 3;
}
