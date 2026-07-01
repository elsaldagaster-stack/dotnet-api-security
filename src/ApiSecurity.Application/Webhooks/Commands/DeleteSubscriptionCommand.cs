using ApiSecurity.Application.Interfaces;
using MediatR;

namespace ApiSecurity.Application.Webhooks.Commands;

public record DeleteSubscriptionCommand(Guid Id) : IRequest<bool>;

public class DeleteSubscriptionCommandHandler(IWebhookRepository repository)
    : IRequestHandler<DeleteSubscriptionCommand, bool>
{
    public async Task<bool> Handle(DeleteSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await repository.FindSubscriptionByIdAsync(request.Id, ct);
        if (subscription is null) return false;

        subscription.Deactivate();
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
