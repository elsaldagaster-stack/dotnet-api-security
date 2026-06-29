using ApiSecurity.Infrastructure.Security;
using FluentAssertions;

namespace ApiSecurity.UnitTests.Security;

public class ApiKeyHasherTests
{
    private readonly ApiKeyHasher _hasher = new();

    [Fact]
    public void GenerateApiKey_ReturnsKeyWithAskPrefix()
    {
        var (plaintext, _, _) = _hasher.GenerateApiKey();
        plaintext.Should().StartWith("ask_");
    }

    [Fact]
    public void GenerateApiKey_PrefixIsFirst8CharsAfterAsk()
    {
        var (plaintext, prefix, _) = _hasher.GenerateApiKey();
        var keyPart = plaintext["ask_".Length..];
        prefix.Should().Be(keyPart[..8]);
    }

    [Fact]
    public void GenerateApiKey_HashMatchesHashKey()
    {
        var (plaintext, _, hash) = _hasher.GenerateApiKey();
        var recomputedHash = _hasher.HashKey(plaintext);
        recomputedHash.Should().Be(hash);
    }

    [Fact]
    public void HashKey_SameInputProducesSameOutput()
    {
        var key = "ask_abc12345xyz";
        _hasher.HashKey(key).Should().Be(_hasher.HashKey(key));
    }

    [Fact]
    public void HashKey_DifferentInputsProduceDifferentHashes()
    {
        var (key1, _, hash1) = _hasher.GenerateApiKey();
        var (key2, _, hash2) = _hasher.GenerateApiKey();
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ExtractPrefix_Returns8CharsFromKeyPart()
    {
        var (plaintext, prefix, _) = _hasher.GenerateApiKey();
        _hasher.ExtractPrefix(plaintext).Should().Be(prefix);
    }

    [Fact]
    public void GenerateApiKey_PrefixLength_Is8()
    {
        var (_, prefix, _) = _hasher.GenerateApiKey();
        prefix.Length.Should().Be(8);
    }
}
