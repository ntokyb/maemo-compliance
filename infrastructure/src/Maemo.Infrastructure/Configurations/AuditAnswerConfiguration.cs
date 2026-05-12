using Maemo.Domain.Audits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class AuditAnswerConfiguration : IEntityTypeConfiguration<AuditAnswer>
{
    public void Configure(EntityTypeBuilder<AuditAnswer> builder)
    {
        builder.ToTable("AuditAnswers");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId)
            .IsRequired();

        builder.Property(a => a.AuditRunId)
            .IsRequired();

        builder.Property(a => a.AuditQuestionId)
            .IsRequired();

        builder.Property(a => a.Score)
            .IsRequired();

        builder.Property(a => a.EvidenceFileUrl)
            .HasMaxLength(500);

        builder.Property(a => a.EvidenceFileHash)
            .HasMaxLength(64); // SHA256 hex string is 64 characters

        builder.Property(a => a.Comment)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.CreatedBy)
            .HasMaxLength(100);

        builder.Property(a => a.ModifiedAt);

        builder.Property(a => a.ModifiedBy)
            .HasMaxLength(100);

        // Foreign key to AuditRun
        builder.HasOne<AuditRun>()
            .WithMany()
            .HasForeignKey(a => a.AuditRunId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to AuditQuestion
        builder.HasOne<AuditQuestion>()
            .WithMany()
            .HasForeignKey(a => a.AuditQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Tenant
        builder.HasOne<Maemo.Domain.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one answer per question per audit run
        builder.HasIndex(a => new { a.AuditRunId, a.AuditQuestionId })
            .IsUnique();

        // Indexes
        builder.HasIndex(a => a.TenantId);
        builder.HasIndex(a => a.AuditRunId);
        builder.HasIndex(a => a.AuditQuestionId);
    }
}

