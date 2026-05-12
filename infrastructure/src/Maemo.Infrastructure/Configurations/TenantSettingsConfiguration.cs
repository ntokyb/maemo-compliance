using Maemo.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.ToTable("TenantSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.TenantId)
            .IsRequired();

        builder.HasIndex(s => s.TenantId);
        builder.HasIndex(s => new { s.TenantId, s.Key })
            .IsUnique();

        // Base entity properties
        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.CreatedBy)
            .HasMaxLength(100);

        builder.Property(s => s.ModifiedAt);

        builder.Property(s => s.ModifiedBy)
            .HasMaxLength(100);
    }
}

