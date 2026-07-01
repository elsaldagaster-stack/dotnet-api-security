using System.Text;
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ApiSecurity.Infrastructure.Webhooks;

public class WebhookDeliveryService(
    IWebhookRepository repository,
    IHttpClientFactory httpClientFactory,
    IWebhookSignatureService signatureService,
    ILogger<WebhookDeliveryService> logger) : IWebhookDeliveryService
{
    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        var deliveries = await repository.GetPendingDeliveriesAsync(ct);

        foreach (var delivery in deliveries)
            await ProcessDeliveryAsync(delivery, ct);

        if (deliveries.Count > 0)
            await repository.SaveChangesAsync(ct);
    }

    private async Task ProcessDeliveryAsync(WebhookDelivery delivery, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("webhook");
        var subscription = delivery.Subscription;
        var signature = signatureService.ComputeSignature(subscription.Secret, delivery.Payload);

        var request = new HttpRequestMessage(HttpMethod.Post, subscription.EndpointUrl)
        {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-Webhook-Signature", signature);
        request.Headers.TryAddWithoutValidation("X-Delivery-Id", delivery.Id.ToString());

        try
        {
            var response = await client.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                delivery.RecordSuccess((int)response.StatusCode);
                logger.LogInformation("Webhook delivered: {DeliveryId}", delivery.Id);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                delivery.RecordFailure((int)response.StatusCode, body);
                logger.LogWarning("Webhook delivery failed: {DeliveryId}, status {Status}", delivery.Id, response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            delivery.RecordFailure(null, ex.Message);
            logger.LogWarning(ex, "Webhook delivery exception: {DeliveryId}", delivery.Id);
        }
    }
}
