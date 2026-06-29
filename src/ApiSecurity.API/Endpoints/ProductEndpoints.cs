using ApiSecurity.API.Authentication;
using ApiSecurity.Application.Products.Commands;
using ApiSecurity.Application.Products.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace ApiSecurity.API.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Products")
            .RequireRateLimiting("apikey-sliding");

        group.MapGet("/", async (IMediator mediator) =>
        {
            var products = await mediator.Send(new ListProductsQuery());
            return Results.Ok(products);
        })
        .RequireAuthorization(new AuthorizeAttribute
        {
            AuthenticationSchemes = $"Bearer,{ApiKeyAuthenticationHandler.SchemeName}"
        })
        .WithSummary("List products — requires JWT Bearer or API key with ReadProducts scope");

        group.MapPost("/", async (CreateProductRequest req, IMediator mediator) =>
        {
            var id = await mediator.Send(new CreateProductCommand(req.Name, req.Price, req.Stock));
            return Results.Created($"/products/{id}", new { id });
        })
        .RequireAuthorization()
        .WithSummary("Create product — requires JWT Bearer (Admin role)");

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var deleted = await mediator.Send(new DeleteProductCommand(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization()
        .WithSummary("Delete product — requires JWT Bearer (Admin role)");

        return app;
    }

    private record CreateProductRequest(string Name, decimal Price, int Stock);
}
