namespace ApiSecurity.Domain.Enums;

public enum AuditEventType
{
    LoginSucceeded,
    LoginFailed,
    TokenRefreshed,
    ApiKeyUsed,
    ApiKeyInvalid,
    ApiKeyRevoked,
    RateLimitExceeded,
    IpBlocked,
    UnauthorizedAccess
}
