using ApiSecurity.API.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace ApiSecurity.UnitTests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    private readonly HttpClient _client;

    public SecurityHeadersMiddlewareTests()
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder => builder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseMiddleware<SecurityHeadersMiddleware>();
                    app.Run(ctx => Task.CompletedTask);
                }))
            .Build();
        host.Start();
        _client = host.GetTestClient();
    }

    [Fact]
    public async Task Response_HasXContentTypeOptionsHeader()
    {
        var response = await _client.GetAsync("/");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [Fact]
    public async Task Response_HasXFrameOptionsDeny()
    {
        var response = await _client.GetAsync("/");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Fact]
    public async Task Response_HasContentSecurityPolicy()
    {
        var response = await _client.GetAsync("/");
        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
    }

    [Fact]
    public async Task Response_HasReferrerPolicy()
    {
        var response = await _client.GetAsync("/");
        response.Headers.GetValues("Referrer-Policy").Should().Contain("strict-origin-when-cross-origin");
    }

    [Fact]
    public async Task Response_HasPermissionsPolicy()
    {
        var response = await _client.GetAsync("/");
        response.Headers.Contains("Permissions-Policy").Should().BeTrue();
    }

    [Fact]
    public async Task Response_DoesNotExposeServerHeader()
    {
        var response = await _client.GetAsync("/");
        response.Headers.Contains("Server").Should().BeFalse();
    }
}
