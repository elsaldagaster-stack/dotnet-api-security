using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public ApiKeyScope Scopes { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    private ApiKey() { }

    public static ApiKey Create(string name, string keyPrefix, string keyHash, ApiKeyScope scopes, string createdBy, DateTime? expiresAt = null)
    {
        return new ApiKey
        {
            Id = Guid.NewGuid(),
            Name = name,
            KeyPrefix = keyPrefix,
            KeyHash = keyHash,
            Scopes = scopes,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            CreatedBy = createdBy
        };
    }

    public void Revoke() => IsRevoked = true;

    public void RecordUsage() => LastUsedAt = DateTime.UtcNow;

    public bool IsValid() => !IsRevoked && (ExpiresAt == null || ExpiresAt > DateTime.UtcNow);

    public bool HasScope(ApiKeyScope scope) => Scopes.HasFlag(scope);
}
