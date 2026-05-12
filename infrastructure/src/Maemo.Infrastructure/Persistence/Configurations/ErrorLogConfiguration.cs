using Maemo.Domain.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ErrorLog entity.
/// </summary>
public class ErrorLogConfiguration : IEntityTypeConfiguration<ErrorLog>
{
    public void Configure(EntityTypeBuilder<ErrorLog> builder)
    {
        builder.ToTable("ErrorLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.Level)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Source)
            .HasMaxLength(200);

        builder.Property(e => e.TenantName)
            .HasMaxLength(200);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.TenantId, e.Timestamp });
    }
}

