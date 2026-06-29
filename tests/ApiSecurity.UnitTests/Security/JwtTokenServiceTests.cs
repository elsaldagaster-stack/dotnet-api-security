using ApiSecurity.Infrastructure.Security;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ApiSecurity.UnitTests.Security;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "super-secret-key-at-least-32-chars-long!!",
                ["Jwt:Issuer"] = "api-security-tests",
                ["Jwt:Audience"] = "api-security-tests",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7"
            })
            .Build();
        _service = new JwtTokenService(config);
    }

    [Fact]
    public void GenerateTokenPair_ReturnsNonEmptyTokens()
    {
        var result = _service.GenerateTokenPair("user-1", "test@example.com", ["Admin"]);
        result.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateTokenPair_AccessTokenExpiresIn15Minutes()
    {
        var before = DateTime.UtcNow;
        var result = _service.GenerateTokenPair("user-1", "test@example.com", []);
        result.AccessTokenExpiry.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ValidateRefreshToken_ReturnsUserIdForValidToken()
    {
        var tokens = _service.GenerateTokenPair("user-42", "test@example.com", []);
        var userId = _service.ValidateRefreshToken(tokens.RefreshToken);
        userId.Should().Be("user-42");
    }

    [Fact]
    public void ValidateRefreshToken_ReturnsNullForGarbage()
    {
        var userId = _service.ValidateRefreshToken("not.a.real.token");
        userId.Should().BeNull();
    }

    [Fact]
    public void GenerateTokenPair_TwoCallsProduceDifferentRefreshTokens()
    {
        var t1 = _service.GenerateTokenPair("user-1", "test@example.com", []);
        var t2 = _service.GenerateTokenPair("user-1", "test@example.com", []);
        t1.RefreshToken.Should().NotBe(t2.RefreshToken);
    }
}
