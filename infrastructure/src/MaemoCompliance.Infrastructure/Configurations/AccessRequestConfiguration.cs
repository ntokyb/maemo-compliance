using MaemoCompliance.Domain.AccessRequests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class AccessRequestConfiguration : IEntityTypeConfiguration<AccessRequest>
{
    public void Configure(EntityTypeBuilder<AccessRequest> builder)
    {
        builder.ToTable("AccessRequests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CompanyName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Industry).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CompanySize).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ContactName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ContactEmail).IsRequired().HasMaxLength(250);
        builder.Property(x => x.ContactRole).IsRequired().HasMaxLength(100);
        builder.Property(x => x.TargetStandardsJson).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.ReferralSource).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ReviewedBy).HasMaxLength(200);
        builder.Property(x => x.RejectionReason).HasMaxLength(2000);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ContactEmail);
        builder.HasIndex(x => x.CreatedAt);
    }
}
