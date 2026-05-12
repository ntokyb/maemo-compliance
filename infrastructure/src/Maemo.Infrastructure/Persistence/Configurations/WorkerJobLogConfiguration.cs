using Maemo.Domain.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for WorkerJobLog entity.
/// </summary>
public class WorkerJobLogConfiguration : IEntityTypeConfiguration<WorkerJobLog>
{
    public void Configure(EntityTypeBuilder<WorkerJobLog> builder)
    {
        builder.ToTable("WorkerJobLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.WorkerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.Source)
            .HasMaxLength(200);

        builder.Property(e => e.TenantName)
            .HasMaxLength(200);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.WorkerName);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.TenantId, e.WorkerName, e.Timestamp });
    }
}

