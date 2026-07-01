using ApiSecurity.Application.Webhooks.Commands;
using ApiSecurity.Application.Webhooks.Queries;
using ApiSecurity.Domain.Enums;
using MediatR;

namespace ApiSecurity.API.Endpoints;

public static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/webhooks")
            .WithTags("Webhooks")
            .RequireRateLimiting("apikey-sliding")
            .RequireAuthorization();

        group.MapPost("/subscriptions", async (CreateSubscriptionRequest req, IMediator mediator) =>
        {
            var result = await mediator.Send(
                new CreateSubscriptionCommand(req.EndpointUrl, req.Secret, req.EventTypes));
            return Results.Created($"/webhooks/subscriptions/{result.Id}", result);
        })
        .WithSummary("Create webhook subscription — requires JWT Bearer");

        group.MapGet("/subscriptions", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetSubscriptionsQuery())))
        .WithSummary("List webhook subscriptions — requires JWT Bearer");

        group.MapDelete("/subscriptions/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var deleted = await mediator.Send(new DeleteSubscriptionCommand(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Deactivate webhook subscription — requires JWT Bearer");

        group.MapGet("/deliveries/{subscriptionId:guid}", async (Guid subscriptionId, IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetDeliveriesQuery(subscriptionId))))
        .WithSummary("Get delivery history for subscription — requires JWT Bearer");

        return app;
    }

    private record CreateSubscriptionRequest(string EndpointUrl, string Secret, WebhookEventType EventTypes);
}
