using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ApiSecurity.Application.ApiKeys.Commands;

public record CreateApiKeyCommand(string Name, ApiKeyScope Scopes, string CreatedBy, DateTime? ExpiresAt = null)
    : IRequest<CreateApiKeyResult>;

public record CreateApiKeyResult(Guid Id, string PlaintextKey, string Name, ApiKeyScope Scopes, DateTime? ExpiresAt);

public class CreateApiKeyCommandHandler(
    IApiKeyRepository repository,
    IApiKeyHasher hasher)
    : IRequestHandler<CreateApiKeyCommand, CreateApiKeyResult>
{
    public async Task<CreateApiKeyResult> Handle(CreateApiKeyCommand request, CancellationToken ct)
    {
        var (plaintext, prefix, hash) = hasher.GenerateApiKey();
        var apiKey = Domain.Entities.ApiKey.Create(request.Name, prefix, hash, request.Scopes, request.CreatedBy, request.ExpiresAt);

        await repository.AddAsync(apiKey, ct);
        await repository.SaveChangesAsync(ct);

        return new CreateApiKeyResult(apiKey.Id, plaintext, apiKey.Name, apiKey.Scopes, apiKey.ExpiresAt);
    }
}

public class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Scopes).IsInEnum();
    }
}
