using Maemo.Domain.AuditLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Persistence.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.EntityType)
            .HasMaxLength(100);

        builder.Property(e => e.PerformedByUserId)
            .HasMaxLength(450);

        builder.Property(e => e.MetadataJson)
            .HasColumnType("text");

        builder.Property(e => e.PerformedAt)
            .IsRequired();

        // Indexes for common query patterns
        builder.HasIndex(e => new { e.TenantId, e.PerformedAt, e.Action })
            .HasDatabaseName("IX_AuditLogs_TenantId_PerformedAt_Action");

        builder.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId })
            .HasDatabaseName("IX_AuditLogs_TenantId_EntityType_EntityId");

        // Prevent updates and deletes at database level (if supported)
        // Note: EF Core doesn't directly support this, but we enforce it at application level
    }
}

