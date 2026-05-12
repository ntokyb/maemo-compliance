using MaemoCompliance.Domain.AuditLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Persistence.Configurations;

public class BusinessAuditLogConfiguration : IEntityTypeConfiguration<BusinessAuditLog>
{
    public void Configure(EntityTypeBuilder<BusinessAuditLog> builder)
    {
        builder.ToTable("BusinessAuditLogs");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TenantId);

        builder.Property(b => b.UserId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Username)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.EntityId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.Timestamp)
            .IsRequired();

        builder.Property(b => b.MetadataJson)
            .HasColumnType("text");

        // Indexes for efficient querying
        builder.HasIndex(b => new { b.TenantId, b.EntityType, b.EntityId, b.Timestamp })
            .HasDatabaseName("IX_BusinessAuditLogs_TenantId_EntityType_EntityId_Timestamp");

        builder.HasIndex(b => new { b.TenantId, b.Action, b.Timestamp })
            .HasDatabaseName("IX_BusinessAuditLogs_TenantId_Action_Timestamp");

        builder.HasIndex(b => new { b.EntityType, b.EntityId })
            .HasDatabaseName("IX_BusinessAuditLogs_EntityType_EntityId");
    }
}

