using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiSecurity.API.Extensions;

public static class RateLimitingExtensions
{
    public const string IpPolicyName = "ip-sliding";
    public const string ApiKeyPolicyName = "apikey-sliding";
    public const string GlobalPolicyName = "global";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddSlidingWindowLimiter(GlobalPolicyName, opt =>
            {
                opt.PermitLimit = 200;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 4;
                opt.QueueLimit = 0;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.AddSlidingWindowLimiter(IpPolicyName, opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 4;
                opt.QueueLimit = 0;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.AddPolicy(ApiKeyPolicyName, context =>
            {
                var apiKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
                var partitionKey = apiKey is not null
                    ? $"apikey:{apiKey[..Math.Min(8, apiKey.Length)]}"
                    : $"ip:{context.Connection.RemoteIpAddress}";

                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = apiKey is not null ? 1000 : 100,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

        return services;
    }
}
