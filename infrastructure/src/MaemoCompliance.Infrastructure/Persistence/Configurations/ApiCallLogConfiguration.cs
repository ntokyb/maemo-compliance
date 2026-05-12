using MaemoCompliance.Domain.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ApiCallLog entity.
/// </summary>
public class ApiCallLogConfiguration : IEntityTypeConfiguration<ApiCallLog>
{
    public void Configure(EntityTypeBuilder<ApiCallLog> builder)
    {
        builder.ToTable("ApiCallLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(e => e.Path)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Source)
            .HasMaxLength(200);

        builder.Property(e => e.TenantName)
            .HasMaxLength(200);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.TenantId, e.Timestamp });
        builder.HasIndex(e => new { e.TenantId, e.StatusCode });
    }
}

