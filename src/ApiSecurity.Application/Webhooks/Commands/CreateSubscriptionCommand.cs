using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ApiSecurity.Application.Webhooks.Commands;

public record CreateSubscriptionCommand(string EndpointUrl, string Secret, WebhookEventType EventTypes)
    : IRequest<CreateSubscriptionResult>;

public record CreateSubscriptionResult(Guid Id, string EndpointUrl, WebhookEventType EventTypes);

public class CreateSubscriptionCommandHandler(IWebhookRepository repository)
    : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResult>
{
    public async Task<CreateSubscriptionResult> Handle(CreateSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = WebhookSubscription.Create(request.EndpointUrl, request.Secret, request.EventTypes);
        await repository.AddSubscriptionAsync(subscription, ct);
        await repository.SaveChangesAsync(ct);
        return new CreateSubscriptionResult(subscription.Id, subscription.EndpointUrl, subscription.EventTypes);
    }
}

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.EndpointUrl).NotEmpty().MaximumLength(500).Must(url =>
            Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
            .WithMessage("EndpointUrl must be a valid HTTP or HTTPS URL.");
        RuleFor(x => x.Secret).NotEmpty().MinimumLength(16).MaximumLength(256);
        RuleFor(x => x.EventTypes).NotEqual(WebhookEventType.None);
    }
}
