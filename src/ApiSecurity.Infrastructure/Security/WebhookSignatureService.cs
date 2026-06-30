using System.Security.Cryptography;
using System.Text;
using ApiSecurity.Application.Interfaces;

namespace ApiSecurity.Infrastructure.Security;

public class WebhookSignatureService : IWebhookSignatureService
{
    public string ComputeSignature(string secret, string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = HMACSHA256.HashData(keyBytes, payloadBytes);
        return $"sha256={Convert.ToHexString(hashBytes).ToLowerInvariant()}";
    }
}
