using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Domain.Entities;

public class WebhookSubscription
{
    public Guid Id { get; private set; }
    public string EndpointUrl { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty;
    public WebhookEventType EventTypes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WebhookSubscription() { }

    public static WebhookSubscription Create(string endpointUrl, string secret, WebhookEventType eventTypes)
        => new()
        {
            Id = Guid.NewGuid(),
            EndpointUrl = endpointUrl,
            Secret = secret,
            EventTypes = eventTypes,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public void Deactivate() => IsActive = false;
}
