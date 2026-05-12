using Maemo.Domain.Common;

namespace Maemo.Domain.Documents;

public class Document : TenantOwnedEntity
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime ReviewDate { get; set; }
    public DocumentStatus Status { get; set; }
    public string? StorageLocation { get; set; }
    public string? FileHash { get; set; } // SHA256 hex string for integrity verification
    
    // Workflow properties
    public DocumentWorkflowState WorkflowState { get; set; } = DocumentWorkflowState.Draft;
    public string? RejectedReason { get; set; }
    
    // POPIA compliance
    public PiiDataType PiiDataType { get; set; } = PiiDataType.None;
    public PersonalInformationType PersonalInformationType { get; set; } = PersonalInformationType.None;
    public PiiType PiiType { get; set; } = PiiType.None;
    public string? PiiDescription { get; set; }
    public int? PiiRetentionPeriodInMonths { get; set; }
    
    // BBBEE certificate tracking
    public DateTime? BbbeeExpiryDate { get; set; }
    public int? BbbeeLevel { get; set; } // 1-8
    
    // Records retention
    public DateTime? RetainUntil { get; set; }
    public bool IsRetentionLocked { get; set; }
    public bool IsPendingArchive { get; set; } // Marked for archiving due to retention expiry
    
    // Versioning properties
    public int Version { get; set; } = 1;
    public string? ApproverUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comments { get; set; }
    public bool IsCurrentVersion { get; set; } = true;
    public Guid? PreviousVersionId { get; set; }

    // Document versions collection
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    
    // Destruction tracking (for retention compliance)
    public bool IsDestroyed { get; set; }
    public DateTime? DestroyedAt { get; set; }
    public string? DestroyedByUserId { get; set; }
    public string? DestroyReason { get; set; }
    
    // File Plan metadata (National Archives compliance)
    public string? FilePlanSeries { get; set; }
    public string? FilePlanSubSeries { get; set; }
    public string? FilePlanItem { get; set; }
}

