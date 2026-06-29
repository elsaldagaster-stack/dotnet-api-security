using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;

namespace ApiSecurity.API.Middleware;

public class AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAuditLogRepository auditRepo)
    {
        await next(context);

        var statusCode = context.Response.StatusCode;
        if (statusCode is 401 or 403 or 429)
        {
            var eventType = statusCode switch
            {
                401 => AuditEventType.UnauthorizedAccess,
                403 => AuditEventType.IpBlocked,
                429 => AuditEventType.RateLimitExceeded,
                _ => AuditEventType.UnauthorizedAccess
            };

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var rawApiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
            var apiKeyPrefix = rawApiKey is not null
                ? rawApiKey[..Math.Min(8, rawApiKey.Length)]
                : null;

            var log = AuditLog.Create(eventType, ip, success: false, userId: userId, apiKeyPrefix: apiKeyPrefix,
                details: $"{context.Request.Method} {context.Request.Path} → {statusCode}");

            try
            {
                await auditRepo.AddAsync(log);
                await auditRepo.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write audit log");
            }
        }
    }
}
