using Maemo.Domain.Documents;

namespace Maemo.Application.Documents.Dtos;

/// <summary>
/// Comprehensive audit evidence for a document, suitable for AGSA audit requirements.
/// </summary>
public class AuditEvidenceDto
{
    public DocumentDto Document { get; set; } = null!;
    public IReadOnlyList<DocumentVersionDto> Versions { get; set; } = new List<DocumentVersionDto>();
    public IReadOnlyList<BusinessAuditLogEntryDto> BusinessAuditLogs { get; set; } = new List<BusinessAuditLogEntryDto>();
    public IReadOnlyList<LinkedNcrDto> LinkedNcrs { get; set; } = new List<LinkedNcrDto>();
    public IReadOnlyList<LinkedRiskDto> LinkedRisks { get; set; } = new List<LinkedRiskDto>();
    public ApprovalHistoryDto? ApprovalHistory { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Business audit log entry for audit evidence.
/// </summary>
public class BusinessAuditLogEntryDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string? Username { get; set; }
    public string? MetadataJson { get; set; }
}

/// <summary>
/// Linked NCR for audit evidence.
/// </summary>
public class LinkedNcrDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Department { get; set; }
    public string? LinkReason { get; set; } // How/why this NCR is linked to the document
}

/// <summary>
/// Linked Risk for audit evidence.
/// </summary>
public class LinkedRiskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? LinkReason { get; set; } // How/why this Risk is linked to the document
}

/// <summary>
/// Approval history for audit evidence.
/// </summary>
public class ApprovalHistoryDto
{
    public string? ApproverUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Comments { get; set; }
    public DocumentWorkflowState CurrentWorkflowState { get; set; }
    public string? RejectedReason { get; set; }
}

