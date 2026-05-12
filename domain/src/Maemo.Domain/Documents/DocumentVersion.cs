using Maemo.Domain.Common;

namespace Maemo.Domain.Documents;

/// <summary>
/// Represents a version of a document file.
/// Each document can have multiple versions, with only one marked as IsLatest.
/// </summary>
public class DocumentVersion : BaseEntity
{
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = null!;
    public string StorageLocation { get; set; } = null!;
    public string? FileHash { get; set; } // SHA256 hex string for integrity verification
    public string? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Comment { get; set; }
    public bool IsLatest { get; set; }

    // Navigation property
    public Document Document { get; set; } = null!;
}

