namespace MapleWindow.App.ViewModels;

public sealed record CharacterPickerItem(string Ocid, string CharacterName, string WorldName, string CharacterClass, long CharacterLevel)
{
    public string DisplayText => $"{CharacterName} ({WorldName}) - {CharacterClass} Lv.{CharacterLevel}";
}
