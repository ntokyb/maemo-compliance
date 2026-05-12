using MaemoCompliance.Domain.Documents;

namespace MaemoCompliance.Application.Documents.Dtos;

public class DocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime ReviewDate { get; set; }
    public DocumentStatus Status { get; set; }
    public DocumentWorkflowState WorkflowState { get; set; }
    public string? RejectedReason { get; set; }
    public PiiDataType PiiDataType { get; set; }
    public PersonalInformationType PersonalInformationType { get; set; }
    public PiiType PiiType { get; set; }
    public string? PiiDescription { get; set; }
    public int? PiiRetentionPeriodInMonths { get; set; }
    public DateTime? BbbeeExpiryDate { get; set; }
    public int? BbbeeLevel { get; set; }
    public DateTime? RetainUntil { get; set; }
    public bool IsRetentionLocked { get; set; }
    public bool IsPendingArchive { get; set; }
    public int Version { get; set; }
    public string? ApproverUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comments { get; set; } // Approval comments
    public bool IsCurrentVersion { get; set; }
    public Guid? PreviousVersionId { get; set; }
    public string? StorageLocation { get; set; }
    
    // File Plan metadata (National Archives compliance)
    public string? FilePlanSeries { get; set; }
    public string? FilePlanSubSeries { get; set; }
    public string? FilePlanItem { get; set; }
    
    // Versioning information
    public int LatestVersionNumber { get; set; }
    public bool HasVersions { get; set; }
    public DocumentVersionDto? LatestVersion { get; set; }
}

