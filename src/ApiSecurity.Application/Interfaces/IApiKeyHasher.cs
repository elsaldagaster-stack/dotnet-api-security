namespace ApiSecurity.Application.Interfaces;

public interface IApiKeyHasher
{
    (string Plaintext, string Prefix, string Hash) GenerateApiKey();
    string HashKey(string plaintext);
    string ExtractPrefix(string plaintext);
}
