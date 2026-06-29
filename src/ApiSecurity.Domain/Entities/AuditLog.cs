using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Domain.Entities;

public class AuditLog
{
    public long Id { get; private set; }
    public AuditEventType EventType { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string? UserId { get; private set; }
    public string? ApiKeyPrefix { get; private set; }
    public string? Details { get; private set; }
    public bool Success { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(AuditEventType eventType, string ipAddress, bool success, string? userId = null, string? apiKeyPrefix = null, string? details = null)
    {
        return new AuditLog
        {
            EventType = eventType,
            IpAddress = ipAddress,
            Success = success,
            UserId = userId,
            ApiKeyPrefix = apiKeyPrefix,
            Details = details,
            OccurredAt = DateTime.UtcNow
        };
    }
}
