using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;
using ApiSecurity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurity.Infrastructure.Repositories;

public class WebhookRepository(AppDbContext db) : IWebhookRepository
{
    public async Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken ct = default)
        => await db.WebhookSubscriptions.AddAsync(subscription, ct);

    public Task<WebhookSubscription?> FindSubscriptionByIdAsync(Guid id, CancellationToken ct = default)
        => db.WebhookSubscriptions.FindAsync([id], ct).AsTask();

    public Task<List<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(WebhookEventType eventType, CancellationToken ct = default)
        => db.WebhookSubscriptions
            .Where(s => s.IsActive && (s.EventTypes & eventType) != 0)
            .ToListAsync(ct);

    public Task<List<WebhookSubscription>> GetAllSubscriptionsAsync(CancellationToken ct = default)
        => db.WebhookSubscriptions.ToListAsync(ct);

    public async Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken ct = default)
        => await db.WebhookDeliveries.AddAsync(delivery, ct);

    public Task<List<WebhookDelivery>> GetPendingDeliveriesAsync(CancellationToken ct = default)
        => db.WebhookDeliveries
            .Include(d => d.Subscription)
            .Where(d => d.Status == WebhookDeliveryStatus.Pending
                && (d.NextAttemptAt == null || d.NextAttemptAt <= DateTimeOffset.UtcNow))
            .ToListAsync(ct);

    public Task<List<WebhookDelivery>> GetDeliveriesBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
        => db.WebhookDeliveries
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
