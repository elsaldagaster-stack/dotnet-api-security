using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Application.Webhooks;

public class WebhookDispatcher(IWebhookRepository repository) : IWebhookDispatcher
{
    public async Task DispatchAsync(WebhookEventType eventType, string payload, CancellationToken ct = default)
    {
        var subscriptions = await repository.GetActiveSubscriptionsForEventAsync(eventType, ct);

        foreach (var subscription in subscriptions)
        {
            var delivery = WebhookDelivery.Create(subscription.Id, payload);
            await repository.AddDeliveryAsync(delivery, ct);
        }

        if (subscriptions.Count > 0)
            await repository.SaveChangesAsync(ct);
    }
}
