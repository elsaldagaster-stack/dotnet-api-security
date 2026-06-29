using System.Security.Claims;
using System.Text.Encodings.Web;
using ApiSecurity.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ApiSecurity.API.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions { }

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyRepository apiKeyRepository,
    IApiKeyHasher hasher)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "ApiKey";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var rawKey))
            return AuthenticateResult.NoResult();

        var key = rawKey.ToString();
        string prefix;
        try { prefix = hasher.ExtractPrefix(key); }
        catch { return AuthenticateResult.Fail("Invalid API key format"); }

        var apiKey = await apiKeyRepository.FindByPrefixAsync(prefix);
        if (apiKey is null)
            return AuthenticateResult.Fail("API key not found");

        if (!apiKey.IsValid())
            return AuthenticateResult.Fail("API key revoked or expired");

        var keyHash = hasher.HashKey(key);
        if (!string.Equals(apiKey.KeyHash, keyHash, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.Fail("Invalid API key");

        apiKey.RecordUsage();
        await apiKeyRepository.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, apiKey.Name),
            new(ClaimTypes.NameIdentifier, apiKey.Id.ToString()),
            new("api_key_scopes", apiKey.Scopes.ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
