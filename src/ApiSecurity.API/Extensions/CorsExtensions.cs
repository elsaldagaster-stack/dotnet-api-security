namespace ApiSecurity.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "ApiCorsPolicy";

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (allowedOrigins.Length == 0)
                    policy.WithOrigins("http://localhost:3000", "http://localhost:5173");
                else
                    policy.WithOrigins(allowedOrigins);

                policy
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }
}
