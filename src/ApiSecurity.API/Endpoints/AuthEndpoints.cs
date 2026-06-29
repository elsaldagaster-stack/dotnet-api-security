using ApiSecurity.Application.Auth.Commands;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiSecurity.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", async (LoginRequest req, IMediator mediator) =>
        {
            var result = await mediator.Send(new LoginCommand(req.Email, req.Password));
            if (result is null) return Results.Unauthorized();
            return Results.Ok(result);
        })
        .RequireRateLimiting("ip-sliding")
        .WithSummary("Login with email/password — returns JWT + refresh token");

        group.MapPost("/refresh", async (RefreshRequest req, IMediator mediator) =>
        {
            var result = await mediator.Send(new RefreshTokenCommand(req.RefreshToken));
            if (result is null) return Results.Unauthorized();
            return Results.Ok(result);
        })
        .RequireRateLimiting("ip-sliding")
        .WithSummary("Exchange refresh token for new token pair");

        return app;
    }

    private record LoginRequest(string Email, string Password);
    private record RefreshRequest(string RefreshToken);
}
