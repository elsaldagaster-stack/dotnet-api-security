using System.Net;

namespace ApiSecurity.API.Middleware;

public class IpFilterMiddleware(RequestDelegate next, IConfiguration config, ILogger<IpFilterMiddleware> logger)
{
    private readonly HashSet<string> _allowlist = LoadList(config, "IpFilter:Allowlist");
    private readonly HashSet<string> _denylist = LoadList(config, "IpFilter:Denylist");

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

        if (_allowlist.Count > 0 && !_allowlist.Contains(ip))
        {
            logger.LogWarning("IP {Ip} not in allowlist — blocked", ip);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        if (_denylist.Contains(ip))
        {
            logger.LogWarning("IP {Ip} in denylist — blocked", ip);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden");
            return;
        }

        await next(context);
    }

    private static HashSet<string> LoadList(IConfiguration config, string key)
        => config.GetSection(key).Get<string[]>()?.ToHashSet() ?? [];
}
