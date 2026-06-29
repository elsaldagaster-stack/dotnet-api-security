using ApiSecurity.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ApiSecurity.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult?>;

public record LoginResult(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public class LoginCommandHandler(ITokenService tokenService) : IRequestHandler<LoginCommand, LoginResult?>
{
    private const string AdminEmail = "admin@example.com";
    private const string AdminPassword = "Admin123!";

    public Task<LoginResult?> Handle(LoginCommand request, CancellationToken ct)
    {
        if (request.Email != AdminEmail || request.Password != AdminPassword)
            return Task.FromResult<LoginResult?>(null);

        var tokens = tokenService.GenerateTokenPair("admin-001", request.Email, ["Admin"]);
        return Task.FromResult<LoginResult?>(new LoginResult(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry));
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(128);
    }
}
