using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiSecurity.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ApiSecurity.Infrastructure.Security;

public class JwtTokenService(IConfiguration config) : ITokenService
{
    private readonly string _signingKey = config["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey not configured");
    private readonly string _issuer = config["Jwt:Issuer"] ?? "api-security";
    private readonly string _audience = config["Jwt:Audience"] ?? "api-security";
    private readonly int _accessExpiry = int.Parse(config["Jwt:AccessTokenExpiryMinutes"] ?? "15");
    private readonly int _refreshExpiry = int.Parse(config["Jwt:RefreshTokenExpiryDays"] ?? "7");

    public TokenPair GenerateTokenPair(string userId, string email, string[] roles)
    {
        var expiry = DateTime.UtcNow.AddMinutes(_accessExpiry);
        var accessToken = BuildAccessToken(userId, email, roles, expiry);
        var refreshToken = BuildRefreshToken(userId);
        return new TokenPair(accessToken, refreshToken, expiry);
    }

    public string? ValidateRefreshToken(string refreshToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
            var result = handler.ValidateToken(refreshToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience + "-refresh",
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return result.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        catch
        {
            return null;
        }
    }

    private string BuildAccessToken(string userId, string email, string[] roles, DateTime expiry)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(_issuer, _audience, claims, expires: expiry, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string BuildRefreshToken(string userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(_issuer, _audience + "-refresh", claims,
            expires: DateTime.UtcNow.AddDays(_refreshExpiry), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
