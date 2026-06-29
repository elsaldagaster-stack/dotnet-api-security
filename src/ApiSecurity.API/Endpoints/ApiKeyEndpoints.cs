using ApiSecurity.API.Authentication;
using ApiSecurity.Application.ApiKeys.Commands;
using ApiSecurity.Domain.Enums;
using MediatR;

namespace ApiSecurity.API.Endpoints;

public static class ApiKeyEndpoints
{
    public static IEndpointRouteBuilder MapApiKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/apikeys")
            .WithTags("API Keys")
            .RequireAuthorization();

        group.MapPost("/", async (CreateApiKeyRequest req, IMediator mediator, HttpContext ctx) =>
        {
            var userId = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var result = await mediator.Send(new CreateApiKeyCommand(req.Name, req.Scopes, userId, req.ExpiresAt));
            return Results.Ok(result);
        })
        .WithSummary("Create API key — plaintext key returned once, store it safely");

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var revoked = await mediator.Send(new RevokeApiKeyCommand(id));
            return revoked ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Revoke an API key");

        return app;
    }

    private record CreateApiKeyRequest(string Name, ApiKeyScope Scopes, DateTime? ExpiresAt);
}
