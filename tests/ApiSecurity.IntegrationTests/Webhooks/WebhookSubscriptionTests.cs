using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiSecurity.Application.Webhooks.Queries;
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Enums;
using ApiSecurity.Infrastructure.Persistence;
using ApiSecurity.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSecurity.IntegrationTests.Webhooks;

public class WebhookSubscriptionTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private HttpClient _client = null!;
    private string _jwtToken = null!;

    public async Task InitializeAsync()
    {
        await fixture.ApplyMigrationsAsync();
        _client = fixture.CreateClient();

        var loginResp = await _client.PostAsJsonAsync("/auth/login",
            new { Email = "admin@example.com", Password = "Admin123!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();
        _jwtToken = tokens!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateSubscription_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/webhook",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateSubscriptionResult>();
        body!.Id.Should().NotBeEmpty();
        body.EndpointUrl.Should().Be("https://example.com/webhook");
    }

    [Fact]
    public async Task DeleteSubscription_ExistingId_Returns204()
    {
        var createResp = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/to-delete",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });
        var created = await createResp.Content.ReadFromJsonAsync<CreateSubscriptionResult>();

        var deleteResp = await _client.DeleteAsync($"/webhooks/subscriptions/{created!.Id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateProduct_WithActiveSubscription_CreatesDelivery()
    {
        var createResp = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/dispatch-test",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });
        var sub = await createResp.Content.ReadFromJsonAsync<CreateSubscriptionResult>();

        await _client.PostAsJsonAsync("/products", new { Name = "Dispatch Test Product", Price = 9.99m, Stock = 5 });

        var deliveriesResp = await _client.GetAsync($"/webhooks/deliveries/{sub!.Id}");
        deliveriesResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deliveries = await deliveriesResp.Content.ReadFromJsonAsync<List<WebhookDeliveryDto>>();
        deliveries.Should().ContainSingle(d => d.Status == WebhookDeliveryStatus.Pending);
    }

    [Fact]
    public async Task ProcessPending_SuccessfulDelivery_StatusIsDelivered()
    {
        fixture.WebhookFakeHandler.ResponseStatusCode = HttpStatusCode.OK;

        var createResp = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/success-test",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });
        var sub = await createResp.Content.ReadFromJsonAsync<CreateSubscriptionResult>();

        await _client.PostAsJsonAsync("/products", new { Name = "Success Product", Price = 1.00m, Stock = 1 });

        using var scope = fixture.Services.CreateScope();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();
        await deliveryService.ProcessPendingAsync();

        var deliveriesResp = await _client.GetAsync($"/webhooks/deliveries/{sub!.Id}");
        var deliveries = await deliveriesResp.Content.ReadFromJsonAsync<List<WebhookDeliveryDto>>();
        deliveries.Should().ContainSingle(d => d.Status == WebhookDeliveryStatus.Delivered);
    }

    [Fact]
    public async Task ProcessPending_AllAttemptsFail_StatusIsFailed()
    {
        fixture.WebhookFakeHandler.ResponseStatusCode = HttpStatusCode.InternalServerError;

        var createResp = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/fail-test",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });
        var sub = await createResp.Content.ReadFromJsonAsync<CreateSubscriptionResult>();

        await _client.PostAsJsonAsync("/products", new { Name = "Fail Product", Price = 1.00m, Stock = 1 });

        using var scope = fixture.Services.CreateScope();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();

        await deliveryService.ProcessPendingAsync();

        // Reset NextAttemptAt so second attempt is picked up immediately
        using var updateScope = fixture.Services.CreateScope();
        var db = updateScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"WebhookDeliveries\" SET \"NextAttemptAt\" = NOW() - INTERVAL '1 second' WHERE \"Status\" = 0 AND \"SubscriptionId\" = {0}",
            sub!.Id);

        await deliveryService.ProcessPendingAsync();

        using var updateScope2 = fixture.Services.CreateScope();
        var db2 = updateScope2.ServiceProvider.GetRequiredService<AppDbContext>();
        await db2.Database.ExecuteSqlRawAsync(
            "UPDATE \"WebhookDeliveries\" SET \"NextAttemptAt\" = NOW() - INTERVAL '1 second' WHERE \"Status\" = 0 AND \"SubscriptionId\" = {0}",
            sub.Id);

        await deliveryService.ProcessPendingAsync();

        var deliveriesResp = await _client.GetAsync($"/webhooks/deliveries/{sub.Id}");
        var deliveries = await deliveriesResp.Content.ReadFromJsonAsync<List<WebhookDeliveryDto>>();
        deliveries.Should().ContainSingle(d =>
            d.Status == WebhookDeliveryStatus.Failed && d.AttemptCount == 3);
    }

    private record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
    private record CreateSubscriptionResult(Guid Id, string EndpointUrl, WebhookEventType EventTypes);
}
