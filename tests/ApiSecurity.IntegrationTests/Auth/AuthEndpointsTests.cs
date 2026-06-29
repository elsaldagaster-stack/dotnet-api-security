using System.Net;
using System.Net.Http.Json;
using ApiSecurity.IntegrationTests.Fixtures;
using FluentAssertions;

namespace ApiSecurity.IntegrationTests.Auth;

public class AuthEndpointsTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await fixture.ApplyMigrationsAsync();
        _client = fixture.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_WithValidCredentials_Returns200WithTokens()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { Email = "admin@example.com", Password = "Admin123!" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<TokenResponse>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new { Email = "bad@example.com", Password = "wrong123" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidToken_Returns200WithNewTokens()
    {
        var loginResp = await _client.PostAsJsonAsync("/auth/login", new { Email = "admin@example.com", Password = "Admin123!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();

        var refreshResp = await _client.PostAsJsonAsync("/auth/refresh", new { RefreshToken = tokens!.RefreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await refreshResp.Content.ReadFromJsonAsync<TokenResponse>();
        newTokens!.AccessToken.Should().NotBe(tokens.AccessToken);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/auth/refresh", new { RefreshToken = "garbage.token.value" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
