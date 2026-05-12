using MaemoCompliance.Domain.Audits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class AuditFindingConfiguration : IEntityTypeConfiguration<AuditFinding>
{
    public void Configure(EntityTypeBuilder<AuditFinding> builder)
    {
        builder.ToTable("AuditFindings");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.TenantId)
            .IsRequired();

        builder.HasIndex(f => new { f.TenantId, f.AuditRunId });

        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.CreatedBy).HasMaxLength(100);
        builder.Property(f => f.ModifiedBy).HasMaxLength(100);
    }
}
