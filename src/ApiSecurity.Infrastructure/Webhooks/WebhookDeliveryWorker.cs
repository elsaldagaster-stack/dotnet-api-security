using ApiSecurity.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiSecurity.Infrastructure.Webhooks;

public class WebhookDeliveryWorker(
    IServiceProvider services,
    ILogger<WebhookDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var deliveryService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();
                await deliveryService.ProcessPendingAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error in webhook delivery worker");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }
}
