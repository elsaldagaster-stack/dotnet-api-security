namespace ApiSecurity.Application.Interfaces;

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry);

public interface ITokenService
{
    TokenPair GenerateTokenPair(string userId, string email, string[] roles);
    string? ValidateRefreshToken(string refreshToken);
}
