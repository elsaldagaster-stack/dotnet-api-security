using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Enums;
using MediatR;

namespace ApiSecurity.Application.Webhooks.Queries;

public record GetDeliveriesQuery(Guid SubscriptionId) : IRequest<List<WebhookDeliveryDto>>;

public record WebhookDeliveryDto(
    Guid Id,
    WebhookDeliveryStatus Status,
    int AttemptCount,
    int? ResponseCode,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset CreatedAt);

public class GetDeliveriesQueryHandler(IWebhookRepository repository)
    : IRequestHandler<GetDeliveriesQuery, List<WebhookDeliveryDto>>
{
    public async Task<List<WebhookDeliveryDto>> Handle(GetDeliveriesQuery request, CancellationToken ct)
    {
        var deliveries = await repository.GetDeliveriesBySubscriptionAsync(request.SubscriptionId, ct);
        return deliveries.Select(d => new WebhookDeliveryDto(
            d.Id, d.Status, d.AttemptCount, d.ResponseCode, d.LastAttemptAt, d.CreatedAt)).ToList();
    }
}
