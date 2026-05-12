using Maemo.Domain.Audits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class AuditRunConfiguration : IEntityTypeConfiguration<AuditRun>
{
    public void Configure(EntityTypeBuilder<AuditRun> builder)
    {
        builder.ToTable("AuditRuns");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId)
            .IsRequired();

        builder.Property(r => r.AuditTemplateId)
            .IsRequired();

        builder.Property(r => r.StartedAt)
            .IsRequired();

        builder.Property(r => r.CompletedAt);

        builder.Property(r => r.AuditorUserId)
            .HasMaxLength(100);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.CreatedBy)
            .HasMaxLength(100);

        builder.Property(r => r.ModifiedAt);

        builder.Property(r => r.ModifiedBy)
            .HasMaxLength(100);

        // Foreign key to AuditTemplate
        builder.HasOne<AuditTemplate>()
            .WithMany()
            .HasForeignKey(r => r.AuditTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Tenant
        builder.HasOne<Maemo.Domain.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(r => r.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.AuditTemplateId);
        builder.HasIndex(r => r.StartedAt);
        builder.HasIndex(r => r.CompletedAt);
    }
}

