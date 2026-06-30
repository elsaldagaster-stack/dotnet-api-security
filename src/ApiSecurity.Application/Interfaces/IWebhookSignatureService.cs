namespace ApiSecurity.Application.Interfaces;

public interface IWebhookSignatureService
{
    string ComputeSignature(string secret, string payload);
}
