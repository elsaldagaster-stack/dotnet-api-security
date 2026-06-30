using ApiSecurity.Infrastructure.Security;
using FluentAssertions;

namespace ApiSecurity.UnitTests.Webhooks;

public class WebhookSignatureServiceTests
{
    private readonly WebhookSignatureService _service = new();

    [Fact]
    public void ComputeSignature_SameInputs_ReturnsSameSignature()
    {
        var sig1 = _service.ComputeSignature("secret", "{\"id\":1}");
        var sig2 = _service.ComputeSignature("secret", "{\"id\":1}");

        sig1.Should().Be(sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentSecret_ReturnsDifferentSignature()
    {
        var sig1 = _service.ComputeSignature("secret1", "{\"id\":1}");
        var sig2 = _service.ComputeSignature("secret2", "{\"id\":1}");

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentPayload_ReturnsDifferentSignature()
    {
        var sig1 = _service.ComputeSignature("secret", "{\"id\":1}");
        var sig2 = _service.ComputeSignature("secret", "{\"id\":2}");

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeSignature_StartsWithSha256Prefix()
    {
        var sig = _service.ComputeSignature("secret", "payload");

        sig.Should().StartWith("sha256=");
    }
}
