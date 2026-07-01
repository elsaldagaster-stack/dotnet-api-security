using System.Net;

namespace ApiSecurity.IntegrationTests.Fixtures;

public class FakeWebhookHandler : HttpMessageHandler
{
    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(new HttpResponseMessage(ResponseStatusCode));
}
