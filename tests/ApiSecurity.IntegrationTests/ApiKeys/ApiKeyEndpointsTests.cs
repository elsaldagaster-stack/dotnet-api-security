using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiSecurity.Domain.Enums;
using ApiSecurity.IntegrationTests.Fixtures;
using FluentAssertions;

namespace ApiSecurity.IntegrationTests.ApiKeys;

public class ApiKeyEndpointsTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private HttpClient _client = null!;
    private string _jwtToken = null!;

    public async Task InitializeAsync()
    {
        await fixture.ApplyMigrationsAsync();
        _client = fixture.CreateClient();

        var loginResp = await _client.PostAsJsonAsync("/auth/login", new { Email = "admin@example.com", Password = "Admin123!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();
        _jwtToken = tokens!.AccessToken;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateApiKey_WithJwt_ReturnsPlaintextKey()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);

        var response = await _client.PostAsJsonAsync("/apikeys", new
        {
            Name = "Test Key",
            Scopes = ApiKeyScope.ReadProducts,
            ExpiresAt = (DateTime?)null
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>();
        body!.PlaintextKey.Should().StartWith("ask_");
    }

    [Fact]
    public async Task Products_WithValidApiKey_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
        var createResp = await _client.PostAsJsonAsync("/apikeys", new
        {
            Name = "Products Key",
            Scopes = ApiKeyScope.ReadProducts,
            ExpiresAt = (DateTime?)null
        });
        var apiKey = (await createResp.Content.ReadFromJsonAsync<CreateApiKeyResponse>())!.PlaintextKey;

        var readClient = fixture.CreateClient();
        readClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        var response = await readClient.GetAsync("/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Products_WithInvalidApiKey_Returns401()
    {
        var readClient = fixture.CreateClient();
        readClient.DefaultRequestHeaders.Add("X-Api-Key", "ask_invalidkeyvalue12345678");
        var response = await readClient.GetAsync("/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RevokeApiKey_ThenUseIt_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
        var createResp = await _client.PostAsJsonAsync("/apikeys", new
        {
            Name = "To Revoke",
            Scopes = ApiKeyScope.ReadProducts,
            ExpiresAt = (DateTime?)null
        });
        var created = await createResp.Content.ReadFromJsonAsync<CreateApiKeyResponse>();

        await _client.DeleteAsync($"/apikeys/{created!.Id}");

        var readClient = fixture.CreateClient();
        readClient.DefaultRequestHeaders.Add("X-Api-Key", created.PlaintextKey);
        var response = await readClient.GetAsync("/products");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
    private record CreateApiKeyResponse(Guid Id, string PlaintextKey, string Name, ApiKeyScope Scopes, DateTime? ExpiresAt);
}
