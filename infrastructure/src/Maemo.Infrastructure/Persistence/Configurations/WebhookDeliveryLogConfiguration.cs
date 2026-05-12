using Maemo.Domain.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for WebhookDeliveryLog entity.
/// </summary>
public class WebhookDeliveryLogConfiguration : IEntityTypeConfiguration<WebhookDeliveryLog>
{
    public void Configure(EntityTypeBuilder<WebhookDeliveryLog> builder)
    {
        builder.ToTable("WebhookDeliveryLogs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.TargetUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.Source)
            .HasMaxLength(200);

        builder.Property(e => e.TenantName)
            .HasMaxLength(200);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.Timestamp);
        builder.HasIndex(e => new { e.TenantId, e.Success, e.Timestamp });
    }
}

