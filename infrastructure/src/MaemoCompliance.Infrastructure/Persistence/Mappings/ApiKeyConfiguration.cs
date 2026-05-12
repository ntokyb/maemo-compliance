using MaemoCompliance.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Persistence.Mappings;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Name)
            .HasMaxLength(200);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Unique index on Key to ensure no duplicates
        builder.HasIndex(x => x.Key)
            .IsUnique();

        // Index on TenantId for efficient lookups
        builder.HasIndex(x => x.TenantId);

        // Index on IsActive for filtering active keys
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

