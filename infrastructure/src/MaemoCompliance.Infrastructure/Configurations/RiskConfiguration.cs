using MaemoCompliance.Domain.Risks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class RiskConfiguration : IEntityTypeConfiguration<Risk>
{
    public void Configure(EntityTypeBuilder<Risk> builder)
    {
        builder.ToTable("Risks");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Description)
            .IsRequired();

        builder.Property(r => r.Category)
            .IsRequired()
            .HasDefaultValue(RiskCategory.Operational)
            .HasConversion<int>();

        builder.Property(r => r.Cause);

        builder.Property(r => r.Consequences);

        builder.Property(r => r.InherentLikelihood)
            .IsRequired();

        builder.Property(r => r.InherentImpact)
            .IsRequired();

        builder.Property(r => r.InherentScore)
            .IsRequired();

        builder.Property(r => r.ExistingControls);

        builder.Property(r => r.ResidualLikelihood)
            .IsRequired();

        builder.Property(r => r.ResidualImpact)
            .IsRequired();

        builder.Property(r => r.ResidualScore)
            .IsRequired();

        builder.Property(r => r.OwnerUserId)
            .HasMaxLength(200);

        builder.Property(r => r.Status)
            .IsRequired()
            .HasDefaultValue(RiskStatus.Identified)
            .HasConversion<int>();

        // TenantId is required and indexed
        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => new { r.TenantId, r.Category });
        builder.HasIndex(r => new { r.TenantId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.ResidualScore });

        // Base entity properties
        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasMaxLength(100);

        builder.Property(r => r.ModifiedAt);

        builder.Property(r => r.ModifiedBy)
            .HasMaxLength(100);
    }
}

