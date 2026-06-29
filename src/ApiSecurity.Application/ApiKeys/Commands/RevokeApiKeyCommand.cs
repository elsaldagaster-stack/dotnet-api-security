using ApiSecurity.Application.Interfaces;
using MediatR;

namespace ApiSecurity.Application.ApiKeys.Commands;

public record RevokeApiKeyCommand(Guid ApiKeyId) : IRequest<bool>;

public class RevokeApiKeyCommandHandler(IApiKeyRepository repository)
    : IRequestHandler<RevokeApiKeyCommand, bool>
{
    public async Task<bool> Handle(RevokeApiKeyCommand request, CancellationToken ct)
    {
        var key = await repository.FindByIdAsync(request.ApiKeyId, ct);
        if (key is null) return false;

        key.Revoke();
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
