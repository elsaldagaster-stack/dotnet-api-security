using ApiSecurity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurity.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();
        builder.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(256);
        builder.Property(x => x.ApiKeyPrefix).HasMaxLength(8);
        builder.Property(x => x.Details).HasMaxLength(1000);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.EventType);
    }
}
