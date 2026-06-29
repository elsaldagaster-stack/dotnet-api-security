using ApiSecurity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurity.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.KeyPrefix).HasMaxLength(8).IsRequired();
        builder.Property(x => x.KeyHash).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Scopes).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => x.KeyPrefix);
    }
}
