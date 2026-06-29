using System.Net;
using System.Net.Http.Json;
using ApiSecurity.IntegrationTests.Fixtures;
using FluentAssertions;

namespace ApiSecurity.IntegrationTests.RateLimiting;

public class RateLimitingTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await fixture.ApplyMigrationsAsync();
        _client = fixture.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AuthEndpoint_After10Requests_Returns429()
    {
        var tasks = Enumerable.Range(0, 11)
            .Select(_ => _client.PostAsJsonAsync("/auth/login", new { Email = "bad@x.com", Password = "badpass1" }));

        var responses = await Task.WhenAll(tasks);
        responses.Should().Contain(r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }
}
