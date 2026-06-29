using ApiSecurity.Application.Interfaces;
using MediatR;

namespace ApiSecurity.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResult?>;

public class RefreshTokenCommandHandler(ITokenService tokenService) : IRequestHandler<RefreshTokenCommand, LoginResult?>
{
    public Task<LoginResult?> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var userId = tokenService.ValidateRefreshToken(request.RefreshToken);
        if (userId is null) return Task.FromResult<LoginResult?>(null);

        var tokens = tokenService.GenerateTokenPair(userId, "admin@example.com", ["Admin"]);
        return Task.FromResult<LoginResult?>(new LoginResult(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry));
    }
}
