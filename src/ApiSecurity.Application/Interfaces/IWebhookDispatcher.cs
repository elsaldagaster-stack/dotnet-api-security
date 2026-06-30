using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Application.Interfaces;

public interface IWebhookDispatcher
{
    Task DispatchAsync(WebhookEventType eventType, string payload, CancellationToken ct = default);
}
