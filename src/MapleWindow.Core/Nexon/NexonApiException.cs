using System.Net;

namespace MapleWindow.Core.Nexon;

public sealed class NexonApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ErrorName { get; }
    public bool IsRateLimited => StatusCode == HttpStatusCode.TooManyRequests;

    public NexonApiException(HttpStatusCode statusCode, string? errorName, string message)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorName = errorName;
    }
}
