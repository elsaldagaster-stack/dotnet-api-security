using ApiSecurity.IntegrationTests.Fixtures;
using FluentAssertions;

namespace ApiSecurity.IntegrationTests.SecurityHeaders;

public class SecurityHeadersTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await fixture.ApplyMigrationsAsync();
        _client = fixture.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AnyEndpoint_Response_ContainsXContentTypeOptions()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    [Fact]
    public async Task AnyEndpoint_Response_ContainsXFrameOptions()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Fact]
    public async Task AnyEndpoint_Response_ContainsCSP()
    {
        var response = await _client.GetAsync("/health");
        response.Headers.Contains("Content-Security-Policy").Should().BeTrue();
    }
}
