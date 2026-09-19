namespace MapleWindow.Core.Nexon.Models;

public sealed class ApiErrorResponse
{
    public ApiErrorDetail Error { get; set; } = new();
}

public sealed class ApiErrorDetail
{
    public string Name { get; set; } = "";
    public string Message { get; set; } = "";
}
