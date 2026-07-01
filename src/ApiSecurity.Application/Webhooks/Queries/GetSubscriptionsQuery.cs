using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Enums;
using MediatR;

namespace ApiSecurity.Application.Webhooks.Queries;

public record GetSubscriptionsQuery : IRequest<List<WebhookSubscriptionDto>>;

public record WebhookSubscriptionDto(
    Guid Id,
    string EndpointUrl,
    WebhookEventType EventTypes,
    bool IsActive,
    DateTimeOffset CreatedAt);

public class GetSubscriptionsQueryHandler(IWebhookRepository repository)
    : IRequestHandler<GetSubscriptionsQuery, List<WebhookSubscriptionDto>>
{
    public async Task<List<WebhookSubscriptionDto>> Handle(GetSubscriptionsQuery request, CancellationToken ct)
    {
        var subscriptions = await repository.GetAllSubscriptionsAsync(ct);
        return subscriptions.Select(s => new WebhookSubscriptionDto(
            s.Id, s.EndpointUrl, s.EventTypes, s.IsActive, s.CreatedAt)).ToList();
    }
}
