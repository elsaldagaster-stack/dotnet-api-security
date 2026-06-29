using System.Security.Cryptography;
using ApiSecurity.Application.Interfaces;

namespace ApiSecurity.Infrastructure.Security;

public class ApiKeyHasher : IApiKeyHasher
{
    private const string KeyPrefix = "ask_";

    public (string Plaintext, string Prefix, string Hash) GenerateApiKey()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(32);
        var keyPart = Convert.ToBase64String(randomBytes)
            .Replace("+", "A").Replace("/", "B").Replace("=", "C");
        var plaintext = $"{KeyPrefix}{keyPart}";
        var prefix = keyPart[..8];
        var hash = HashKey(plaintext);
        return (plaintext, prefix, hash);
    }

    public string HashKey(string plaintext)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public string ExtractPrefix(string plaintext)
    {
        if (!plaintext.StartsWith(KeyPrefix))
            throw new ArgumentException("Invalid API key format", nameof(plaintext));
        return plaintext[KeyPrefix.Length..(KeyPrefix.Length + 8)];
    }
}
