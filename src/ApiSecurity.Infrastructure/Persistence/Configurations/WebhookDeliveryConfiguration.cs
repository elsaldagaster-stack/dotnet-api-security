using ApiSecurity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurity.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ResponseBody).HasMaxLength(4000);
        builder.HasOne(x => x.Subscription)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}
