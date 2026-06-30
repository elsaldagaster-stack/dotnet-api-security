using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Application.Interfaces;

public interface IWebhookRepository
{
    Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken ct = default);
    Task<WebhookSubscription?> FindSubscriptionByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(WebhookEventType eventType, CancellationToken ct = default);
    Task<List<WebhookSubscription>> GetAllSubscriptionsAsync(CancellationToken ct = default);
    Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken ct = default);
    Task<List<WebhookDelivery>> GetPendingDeliveriesAsync(CancellationToken ct = default);
    Task<List<WebhookDelivery>> GetDeliveriesBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
