using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;
using FluentAssertions;

namespace ApiSecurity.UnitTests.Webhooks;

public class WebhookDeliveryTests
{
    [Fact]
    public void Create_InitialState_IsPendingWithZeroAttempts()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{\"id\":1}");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
        delivery.NextAttemptAt.Should().BeNull();
        delivery.ResponseCode.Should().BeNull();
    }

    [Fact]
    public void RecordSuccess_SetsStatusDelivered()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{}");

        delivery.RecordSuccess(200);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Delivered);
        delivery.ResponseCode.Should().Be(200);
        delivery.LastAttemptAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailure_FirstAttempt_RemainsPendingWithNextAttemptIn5Seconds()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{}");
        var before = DateTimeOffset.UtcNow;

        delivery.RecordFailure(500, "error");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(1);
        delivery.NextAttemptAt.Should().BeCloseTo(before.AddSeconds(5), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_SecondAttempt_NextAttemptIn25Seconds()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{}");
        delivery.RecordFailure(500, null);
        var before = DateTimeOffset.UtcNow;

        delivery.RecordFailure(500, null);

        delivery.AttemptCount.Should().Be(2);
        delivery.NextAttemptAt.Should().BeCloseTo(before.AddSeconds(25), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_ThirdAttempt_StatusIsFailedAndNoNextAttempt()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{}");
        delivery.RecordFailure(500, null);
        delivery.RecordFailure(500, null);

        delivery.RecordFailure(500, "final error");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Failed);
        delivery.AttemptCount.Should().Be(3);
        delivery.NextAttemptAt.Should().BeNull();
        delivery.ResponseBody.Should().Be("final error");
    }
}
