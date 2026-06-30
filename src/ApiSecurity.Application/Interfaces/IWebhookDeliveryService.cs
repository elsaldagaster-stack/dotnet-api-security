namespace ApiSecurity.Application.Interfaces;

public interface IWebhookDeliveryService
{
    Task ProcessPendingAsync(CancellationToken ct = default);
}
