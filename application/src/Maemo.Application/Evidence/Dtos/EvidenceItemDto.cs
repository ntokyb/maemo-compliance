namespace Maemo.Application.Evidence.Dtos;

/// <summary>
/// DTO representing an evidence item in the Evidence Register.
/// </summary>
public class EvidenceItemDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = null!; // Document, DocumentVersion, AuditAnswer, etc.
    public string EntityId { get; set; } = null!; // ID of the parent entity (DocumentId, AuditRunId, etc.)
    public string FileName { get; set; } = null!;
    public string StorageLocation { get; set; } = null!;
    public string? FileHash { get; set; } // SHA256 hex string
    public DateTime UploadedAt { get; set; }
    public string? UploadedBy { get; set; }
    public Guid? TenantId { get; set; }
    
    // Additional context fields
    public string? DocumentTitle { get; set; } // For Document/DocumentVersion
    public Guid? AuditRunId { get; set; } // For AuditAnswer
    public Guid? AuditQuestionId { get; set; } // For AuditAnswer
    public int? VersionNumber { get; set; } // For DocumentVersion
}

