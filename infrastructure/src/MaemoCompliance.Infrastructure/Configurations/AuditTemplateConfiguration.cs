using MaemoCompliance.Domain.Audits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class AuditTemplateConfiguration : IEntityTypeConfiguration<AuditTemplate>
{
    public void Configure(EntityTypeBuilder<AuditTemplate> builder)
    {
        builder.ToTable("AuditTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ConsultantUserId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100);

        builder.Property(t => t.ModifiedAt);

        builder.Property(t => t.ModifiedBy)
            .HasMaxLength(100);

        // Foreign key to User (Consultant)
        builder.HasOne<MaemoCompliance.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(t => t.ConsultantUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(t => t.ConsultantUserId);
        builder.HasIndex(t => t.Name);
    }
}

