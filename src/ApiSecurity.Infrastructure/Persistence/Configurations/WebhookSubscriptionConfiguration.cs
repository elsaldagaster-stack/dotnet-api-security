using ApiSecurity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurity.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EndpointUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Secret).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EventTypes).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);
    }
}
