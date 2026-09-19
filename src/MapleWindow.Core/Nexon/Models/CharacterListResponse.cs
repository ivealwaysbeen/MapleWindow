namespace MapleWindow.Core.Nexon.Models;

public sealed class CharacterListResponse
{
    public List<AccountEntry> AccountList { get; set; } = [];
}

public sealed class AccountEntry
{
    public string AccountId { get; set; } = "";
    public List<CharacterSummary> CharacterList { get; set; } = [];
}

public sealed class CharacterSummary
{
    public string Ocid { get; set; } = "";
    public string CharacterName { get; set; } = "";
    public string WorldName { get; set; } = "";
    public string CharacterClass { get; set; } = "";
    public long CharacterLevel { get; set; }
}
