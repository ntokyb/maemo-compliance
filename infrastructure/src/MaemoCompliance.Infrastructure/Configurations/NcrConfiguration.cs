using MaemoCompliance.Domain.Ncrs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class NcrConfiguration : IEntityTypeConfiguration<Ncr>
{
    public void Configure(EntityTypeBuilder<Ncr> builder)
    {
        builder.ToTable("Ncrs");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Description)
            .IsRequired();

        builder.Property(n => n.Department)
            .HasMaxLength(100);

        builder.Property(n => n.OwnerUserId)
            .HasMaxLength(200);

        builder.Property(n => n.Severity)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(n => n.Status)
            .IsRequired()
            .HasDefaultValue(NcrStatus.Open)
            .HasConversion<int>();

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.DueDate);

        builder.Property(n => n.ClosedAt);

        // Phase 2 enhancements
        builder.Property(n => n.Category)
            .IsRequired()
            .HasDefaultValue(NcrCategory.Process)
            .HasConversion<int>();

        builder.Property(n => n.RootCause);

        builder.Property(n => n.CorrectiveAction);

        builder.Property(n => n.EscalationLevel)
            .IsRequired()
            .HasDefaultValue(0);

        // TenantId is required and indexed
        builder.Property(n => n.TenantId)
            .IsRequired();

        builder.HasIndex(n => n.TenantId);

        // Additional indexes for common queries
        builder.HasIndex(n => new { n.TenantId, n.Status });
        builder.HasIndex(n => new { n.TenantId, n.Severity });
        builder.HasIndex(n => new { n.TenantId, n.DueDate });
        builder.HasIndex(n => new { n.TenantId, n.Department });

        // Base entity properties
        builder.Property(n => n.CreatedBy)
            .HasMaxLength(100);

        builder.Property(n => n.ModifiedAt);

        builder.Property(n => n.ModifiedBy)
            .HasMaxLength(100);
    }
}

