using MaemoCompliance.Domain.Audits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class AuditProgrammeConfiguration : IEntityTypeConfiguration<AuditProgramme>
{
    public void Configure(EntityTypeBuilder<AuditProgramme> builder)
    {
        builder.ToTable("AuditProgrammes");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Year)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.TenantId)
            .IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.Year });

        builder.HasMany(p => p.Items)
            .WithOne(i => i.AuditProgramme)
            .HasForeignKey(i => i.AuditProgrammeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.CreatedBy).HasMaxLength(100);
        builder.Property(p => p.ModifiedBy).HasMaxLength(100);
    }
}
