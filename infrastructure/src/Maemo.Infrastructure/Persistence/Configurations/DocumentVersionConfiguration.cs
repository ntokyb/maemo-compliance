using Maemo.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Maemo.Infrastructure.Persistence.Configurations;

public class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("DocumentVersions");

        builder.HasKey(dv => dv.Id);

        builder.Property(dv => dv.DocumentId)
            .IsRequired();

        builder.Property(dv => dv.VersionNumber)
            .IsRequired();

        builder.Property(dv => dv.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(dv => dv.StorageLocation)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(dv => dv.FileHash)
            .HasMaxLength(64); // SHA256 hex string is 64 characters

        builder.Property(dv => dv.UploadedBy)
            .HasMaxLength(200);

        builder.Property(dv => dv.UploadedAt)
            .IsRequired();

        builder.Property(dv => dv.Comment)
            .HasMaxLength(2000);

        builder.Property(dv => dv.IsLatest)
            .IsRequired()
            .HasDefaultValue(false);

        // Index on (DocumentId, VersionNumber) for efficient version queries
        builder.HasIndex(dv => new { dv.DocumentId, dv.VersionNumber })
            .IsUnique()
            .HasDatabaseName("IX_DocumentVersions_DocumentId_VersionNumber");

        // Index on DocumentId and IsLatest for finding latest version
        builder.HasIndex(dv => new { dv.DocumentId, dv.IsLatest })
            .HasDatabaseName("IX_DocumentVersions_DocumentId_IsLatest");

        // Foreign key relationship
        builder.HasOne(dv => dv.Document)
            .WithMany(d => d.Versions)
            .HasForeignKey(dv => dv.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure only one IsLatest = true per document
        // This is enforced at the application level, but we can add a check constraint in SQL if needed
    }
}

