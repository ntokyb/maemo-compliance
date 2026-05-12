using Maemo.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class ConsultantTenantLinkConfiguration : IEntityTypeConfiguration<ConsultantTenantLink>
{
    public void Configure(EntityTypeBuilder<ConsultantTenantLink> builder)
    {
        builder.ToTable("ConsultantTenantLinks");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ConsultantUserId)
            .IsRequired();

        builder.Property(l => l.TenantId)
            .IsRequired();

        builder.Property(l => l.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.CreatedBy)
            .HasMaxLength(100);

        builder.Property(l => l.ModifiedAt);

        builder.Property(l => l.ModifiedBy)
            .HasMaxLength(100);

        // Foreign keys
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.ConsultantUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Maemo.Domain.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(l => l.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index to prevent duplicate links
        builder.HasIndex(l => new { l.TenantId, l.ConsultantUserId })
            .IsUnique();

        // Indexes
        builder.HasIndex(l => l.ConsultantUserId);
        builder.HasIndex(l => l.TenantId);
        builder.HasIndex(l => l.IsActive);
    }
}

