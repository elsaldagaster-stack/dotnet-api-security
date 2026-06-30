using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Domain.Entities;

public class WebhookDelivery
{
    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public WebhookSubscription Subscription { get; private set; } = null!;
    public string Payload { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public WebhookDeliveryStatus Status { get; private set; }
    public int? ResponseCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WebhookDelivery() { }

    public static WebhookDelivery Create(Guid subscriptionId, string payload)
        => new()
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Payload = payload,
            AttemptCount = 0,
            Status = WebhookDeliveryStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public void RecordSuccess(int responseCode)
    {
        Status = WebhookDeliveryStatus.Delivered;
        ResponseCode = responseCode;
        LastAttemptAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailure(int? responseCode, string? responseBody)
    {
        AttemptCount++;
        ResponseCode = responseCode;
        ResponseBody = responseBody;
        LastAttemptAt = DateTimeOffset.UtcNow;

        if (AttemptCount >= 3)
        {
            Status = WebhookDeliveryStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            var delaySeconds = Math.Pow(5, AttemptCount);
            NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        }
    }
}
