using Maemo.Domain.Ncrs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Configurations;

public class NcrStatusHistoryConfiguration : IEntityTypeConfiguration<NcrStatusHistory>
{
    public void Configure(EntityTypeBuilder<NcrStatusHistory> builder)
    {
        builder.ToTable("NcrStatusHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.NcrId)
            .IsRequired();

        builder.Property(h => h.OldStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.NewStatus)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.ChangedAt)
            .IsRequired();

        builder.Property(h => h.ChangedByUserId)
            .HasMaxLength(200);

        // TenantId is required and indexed
        builder.Property(h => h.TenantId)
            .IsRequired();

        builder.HasIndex(h => new { h.NcrId, h.TenantId });

        // Base entity properties
        builder.Property(h => h.CreatedAt)
            .IsRequired();

        builder.Property(h => h.CreatedBy)
            .HasMaxLength(100);

        builder.Property(h => h.ModifiedAt);

        builder.Property(h => h.ModifiedBy)
            .HasMaxLength(100);
    }
}

