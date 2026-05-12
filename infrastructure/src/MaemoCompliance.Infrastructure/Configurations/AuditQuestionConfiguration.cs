using MaemoCompliance.Domain.Audits;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class AuditQuestionConfiguration : IEntityTypeConfiguration<AuditQuestion>
{
    public void Configure(EntityTypeBuilder<AuditQuestion> builder)
    {
        builder.ToTable("AuditQuestions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.AuditTemplateId)
            .IsRequired();

        builder.Property(q => q.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(q => q.QuestionText)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(q => q.MaxScore)
            .IsRequired();

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        builder.Property(q => q.CreatedBy)
            .HasMaxLength(100);

        builder.Property(q => q.ModifiedAt);

        builder.Property(q => q.ModifiedBy)
            .HasMaxLength(100);

        // Foreign key to AuditTemplate
        builder.HasOne<AuditTemplate>()
            .WithMany()
            .HasForeignKey(q => q.AuditTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(q => q.AuditTemplateId);
        builder.HasIndex(q => q.Category);
    }
}

