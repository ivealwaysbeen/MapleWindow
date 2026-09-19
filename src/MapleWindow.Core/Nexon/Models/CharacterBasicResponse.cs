namespace MapleWindow.Core.Nexon.Models;

public sealed class CharacterBasicResponse
{
    public string Date { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string WorldName { get; set; } = "";
    public string CharacterGender { get; set; } = "";
    public string CharacterClass { get; set; } = "";
    public string CharacterClassLevel { get; set; } = "";
    public long CharacterLevel { get; set; }
    public long CharacterExp { get; set; }
    public string CharacterExpRate { get; set; } = "";
    public string? CharacterGuildName { get; set; }
    public string CharacterImage { get; set; } = "";
    public string CharacterDateCreate { get; set; } = "";
    public string AccessFlag { get; set; } = "";
    public string LiberationQuestClear { get; set; } = "";
}
