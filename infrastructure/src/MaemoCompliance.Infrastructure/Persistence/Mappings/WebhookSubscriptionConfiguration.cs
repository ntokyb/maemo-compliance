using MaemoCompliance.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Persistence.Mappings;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("WebhookSubscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId)
            .IsRequired();

        builder.Property(x => x.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Secret)
            .HasMaxLength(256);

        builder.Property(x => x.IsActive)
            .IsRequired();

        // Index on TenantId + EventType for efficient lookups
        builder.HasIndex(x => new { x.TenantId, x.EventType });

        // Index on TenantId + IsActive for filtering active subscriptions
        builder.HasIndex(x => new { x.TenantId, x.IsActive });
    }
}

