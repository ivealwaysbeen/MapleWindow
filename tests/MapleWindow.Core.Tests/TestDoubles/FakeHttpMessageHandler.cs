using System.Net;

namespace MapleWindow.Core.Tests.TestDoubles;

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public List<string> RequestedUrls { get; } = [];
    public byte[] ResponseBytes { get; set; } = [1, 2, 3, 4];
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        RequestedUrls.Add(request.RequestUri!.ToString());
        var response = new HttpResponseMessage(StatusCode) { Content = new ByteArrayContent(ResponseBytes) };
        return Task.FromResult(response);
    }
}
