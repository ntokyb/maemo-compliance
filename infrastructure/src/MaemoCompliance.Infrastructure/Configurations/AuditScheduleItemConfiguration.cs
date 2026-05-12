using MaemoCompliance.Domain.Audits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class AuditScheduleItemConfiguration : IEntityTypeConfiguration<AuditScheduleItem>
{
    public void Configure(EntityTypeBuilder<AuditScheduleItem> builder)
    {
        builder.ToTable("AuditScheduleItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProcessArea)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.AuditorName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.PlannedDate)
            .IsRequired();

        builder.Property(i => i.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.TenantId)
            .IsRequired();

        builder.HasIndex(i => new { i.TenantId, i.AuditProgrammeId });

        builder.Property(i => i.CreatedAt).IsRequired();
        builder.Property(i => i.CreatedBy).HasMaxLength(100);
        builder.Property(i => i.ModifiedBy).HasMaxLength(100);
    }
}
