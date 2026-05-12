using MaemoCompliance.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaemoCompliance.Infrastructure.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        // Global query filter: exclude destroyed documents by default
        // Admin queries can use IgnoreQueryFilters() to include destroyed documents
        builder.HasQueryFilter(d => !d.IsDestroyed);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Category)
            .HasMaxLength(100);

        builder.Property(d => d.Department)
            .HasMaxLength(100);

        builder.Property(d => d.OwnerUserId)
            .HasMaxLength(200);

        builder.Property(d => d.ReviewDate)
            .IsRequired();

        builder.Property(d => d.Status)
            .IsRequired()
            .HasDefaultValue(DocumentStatus.Draft)
            .HasConversion<int>();

        builder.Property(d => d.WorkflowState)
            .IsRequired()
            .HasDefaultValue(DocumentWorkflowState.Draft)
            .HasConversion<int>();

        builder.Property(d => d.RejectedReason)
            .HasMaxLength(1000);

        builder.Property(d => d.PiiDataType)
            .IsRequired()
            .HasDefaultValue(PiiDataType.None)
            .HasConversion<int>();

        builder.Property(d => d.PersonalInformationType)
            .IsRequired()
            .HasDefaultValue(PersonalInformationType.None)
            .HasConversion<int>();

        builder.Property(d => d.PiiType)
            .IsRequired()
            .HasDefaultValue(PiiType.None)
            .HasConversion<int>();

        builder.Property(d => d.PiiDescription)
            .HasMaxLength(1000);

        builder.Property(d => d.PiiRetentionPeriodInMonths);

        builder.Property(d => d.BbbeeExpiryDate);

        builder.Property(d => d.BbbeeLevel);

        // Records retention
        builder.Property(d => d.RetainUntil);
        builder.Property(d => d.IsRetentionLocked)
            .IsRequired()
            .HasDefaultValue(false);
        builder.Property(d => d.IsPendingArchive)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.StorageLocation)
            .HasMaxLength(500);

        builder.Property(d => d.FileHash)
            .HasMaxLength(64); // SHA256 hex string is 64 characters

        // Versioning properties
        builder.Property(d => d.Version)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(d => d.ApproverUserId)
            .HasMaxLength(200);

        builder.Property(d => d.ApprovedBy)
            .HasMaxLength(200);

        builder.Property(d => d.ApprovedAt);

        builder.Property(d => d.SubmittedForReviewAt);

        builder.Property(d => d.SupersededByDocumentId);

        builder.Property(d => d.Comments)
            .HasMaxLength(2000);

        builder.Property(d => d.IsCurrentVersion)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(d => d.PreviousVersionId);

        // TenantId is required and indexed
        builder.Property(d => d.TenantId)
            .IsRequired();

        builder.HasIndex(d => d.TenantId);

        // Additional indexes for common queries
        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => new { d.TenantId, d.WorkflowState });
        builder.HasIndex(d => new { d.TenantId, d.Department });
        builder.HasIndex(d => new { d.TenantId, d.PiiDataType });
        builder.HasIndex(d => new { d.TenantId, d.PersonalInformationType });
        builder.HasIndex(d => new { d.TenantId, d.Category, d.BbbeeExpiryDate });
        
        // Index for retention queries
        builder.HasIndex(d => new { d.TenantId, d.RetainUntil, d.IsRetentionLocked });
        
        // Index for querying current versions per document
        builder.HasIndex(d => new { d.TenantId, d.Title, d.IsCurrentVersion });

        // Destruction tracking
        builder.Property(d => d.IsDestroyed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.DestroyedAt);

        builder.Property(d => d.DestroyedByUserId)
            .HasMaxLength(200);

        builder.Property(d => d.DestroyReason)
            .HasMaxLength(1000);

        // Index for filtering destroyed documents
        builder.HasIndex(d => new { d.TenantId, d.IsDestroyed });

        // File Plan metadata (National Archives compliance)
        builder.Property(d => d.FilePlanSeries)
            .HasMaxLength(100);

        builder.Property(d => d.FilePlanSubSeries)
            .HasMaxLength(100);

        builder.Property(d => d.FilePlanItem)
            .HasMaxLength(100);

        // Index for file plan queries
        builder.HasIndex(d => new { d.TenantId, d.FilePlanSeries, d.FilePlanSubSeries });

        // Base entity properties
        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasMaxLength(100);

        builder.Property(d => d.ModifiedAt);

        builder.Property(d => d.ModifiedBy)
            .HasMaxLength(100);
    }
}

