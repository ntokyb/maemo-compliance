using Maemo.Domain.Ncrs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class NcrRiskLinkConfiguration : IEntityTypeConfiguration<NcrRiskLink>
{
    public void Configure(EntityTypeBuilder<NcrRiskLink> builder)
    {
        builder.ToTable("NcrRiskLinks");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.NcrId)
            .IsRequired();

        builder.Property(l => l.RiskId)
            .IsRequired();

        // TenantId is required and indexed
        builder.Property(l => l.TenantId)
            .IsRequired();

        // Unique index to prevent duplicate links
        builder.HasIndex(l => new { l.TenantId, l.NcrId, l.RiskId })
            .IsUnique();

        // Indexes for efficient queries
        builder.HasIndex(l => new { l.TenantId, l.NcrId });
        builder.HasIndex(l => new { l.TenantId, l.RiskId });

        // Base entity properties
        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.CreatedBy)
            .HasMaxLength(100);

        builder.Property(l => l.ModifiedAt);

        builder.Property(l => l.ModifiedBy)
            .HasMaxLength(100);
    }
}

