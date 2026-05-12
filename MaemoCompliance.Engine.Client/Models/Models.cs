// This file contains all the DTOs and enums used by the Maemo Engine Client
// These mirror the types from MaemoCompliance.Application but are kept separate for SDK independence

namespace MaemoCompliance.Engine.Client.Models;

#region Enums

/// <summary>
/// Document status enumeration.
/// </summary>
public enum DocumentStatus
{
    Draft = 0,
    UnderReview = 1,
    Active = 2,
    Archived = 3
}

/// <summary>
/// NCR status enumeration.
/// </summary>
public enum NcrStatus
{
    Open = 0,
    InProgress = 1,
    Closed = 2
}

/// <summary>
/// NCR severity enumeration.
/// </summary>
public enum NcrSeverity
{
    Low = 0,
    Medium = 1,
    High = 2
}

/// <summary>
/// NCR category enumeration.
/// </summary>
public enum NcrCategory
{
    Process = 0,
    Product = 1,
    System = 2,
    Other = 3
}

/// <summary>
/// Risk category enumeration.
/// </summary>
public enum RiskCategory
{
    Operational = 0,
    Financial = 1,
    Compliance = 2,
    HealthSafety = 3,
    InformationSecurity = 4
}

/// <summary>
/// Risk status enumeration.
/// </summary>
public enum RiskStatus
{
    Identified = 0,
    Analysed = 1,
    Mitigated = 2,
    Accepted = 3,
    Closed = 4
}

#endregion

#region Document DTOs

/// <summary>
/// Document data transfer object.
/// </summary>
public class DocumentDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime ReviewDate { get; set; }
    public DocumentStatus Status { get; set; }
    public int Version { get; set; }
    public string? ApproverUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool IsCurrentVersion { get; set; }
    public Guid? PreviousVersionId { get; set; }
}

/// <summary>
/// Request for creating a new document.
/// </summary>
public class CreateDocumentRequest
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime ReviewDate { get; set; }
}

/// <summary>
/// Request for updating an existing document.
/// </summary>
public class UpdateDocumentRequest
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime ReviewDate { get; set; }
    public DocumentStatus Status { get; set; }
}

/// <summary>
/// Request for creating a new document version.
/// </summary>
public class CreateNewDocumentVersionRequest
{
    public string Title { get; set; } = null!;
    public string? Category { get; set; }
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public DateTime ReviewDate { get; set; }
    public string? Comments { get; set; }
}

/// <summary>
/// Request for changing document status.
/// </summary>
public class ChangeDocumentStatusRequest
{
    public DocumentStatus Status { get; set; }
    public string? ApproverUserId { get; set; }
}

/// <summary>
/// Response from creating a document.
/// </summary>
public class CreateDocumentResponse
{
    public Guid Id { get; set; }
}

/// <summary>
/// Response from uploading a file.
/// </summary>
public class UploadFileResponse
{
    public string StorageLocation { get; set; } = null!;
}

#endregion

#region NCR DTOs

/// <summary>
/// Non-Conformance Report data transfer object.
/// </summary>
public class NcrDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public NcrSeverity Severity { get; set; }
    public NcrStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    public NcrCategory Category { get; set; }
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public int EscalationLevel { get; set; }
}

/// <summary>
/// Request for creating a new NCR.
/// </summary>
public class CreateNcrRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public NcrSeverity Severity { get; set; }
    public DateTime? DueDate { get; set; }
    public NcrCategory Category { get; set; } = NcrCategory.Process;
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public int EscalationLevel { get; set; } = 0;
}

/// <summary>
/// Request for updating NCR status.
/// </summary>
public class UpdateNcrStatusRequest
{
    public NcrStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
}

/// <summary>
/// NCR status history entry.
/// </summary>
public class NcrStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid NcrId { get; set; }
    public NcrStatus OldStatus { get; set; }
    public NcrStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByUserId { get; set; }
}

/// <summary>
/// Response from creating an NCR.
/// </summary>
public class CreateNcrResponse
{
    public Guid Id { get; set; }
}

#endregion

#region Risk DTOs

/// <summary>
/// Risk data transfer object.
/// </summary>
public class RiskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public RiskCategory Category { get; set; }
    public string? Cause { get; set; }
    public string? Consequences { get; set; }
    public int InherentLikelihood { get; set; }
    public int InherentImpact { get; set; }
    public int InherentScore { get; set; }
    public string? ExistingControls { get; set; }
    public int ResidualLikelihood { get; set; }
    public int ResidualImpact { get; set; }
    public int ResidualScore { get; set; }
    public string? OwnerUserId { get; set; }
    public RiskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string RiskLevel { get; set; } = null!;
}

/// <summary>
/// Request for creating a new risk.
/// </summary>
public class CreateRiskRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public RiskCategory Category { get; set; }
    public string? Cause { get; set; }
    public string? Consequences { get; set; }
    public int InherentLikelihood { get; set; }
    public int InherentImpact { get; set; }
    public string? ExistingControls { get; set; }
    public int ResidualLikelihood { get; set; }
    public int ResidualImpact { get; set; }
    public string? OwnerUserId { get; set; }
    public RiskStatus Status { get; set; } = RiskStatus.Identified;
}

/// <summary>
/// Request for updating an existing risk.
/// </summary>
public class UpdateRiskRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public RiskCategory Category { get; set; }
    public string? Cause { get; set; }
    public string? Consequences { get; set; }
    public int InherentLikelihood { get; set; }
    public int InherentImpact { get; set; }
    public string? ExistingControls { get; set; }
    public int ResidualLikelihood { get; set; }
    public int ResidualImpact { get; set; }
    public string? OwnerUserId { get; set; }
    public RiskStatus Status { get; set; }
}

/// <summary>
/// Response from creating a risk.
/// </summary>
public class CreateRiskResponse
{
    public Guid Id { get; set; }
}

#endregion

#region Audit DTOs

/// <summary>
/// Audit template data transfer object.
/// </summary>
public class AuditTemplateDto
{
    public Guid Id { get; set; }
    public Guid ConsultantUserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Audit question data transfer object.
/// </summary>
public class AuditQuestionDto
{
    public Guid Id { get; set; }
    public Guid AuditTemplateId { get; set; }
    public string Category { get; set; } = null!;
    public string QuestionText { get; set; } = null!;
    public int MaxScore { get; set; }
}

/// <summary>
/// Audit run data transfer object.
/// </summary>
public class AuditRunDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AuditTemplateId { get; set; }
    public string? TemplateName { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AuditorUserId { get; set; }
}

/// <summary>
/// Audit answer data transfer object.
/// </summary>
public class AuditAnswerDto
{
    public Guid Id { get; set; }
    public Guid AuditRunId { get; set; }
    public Guid AuditQuestionId { get; set; }
    public string? QuestionText { get; set; }
    public string? Category { get; set; }
    public int Score { get; set; }
    public int? MaxScore { get; set; }
    public string? EvidenceFileUrl { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// Request for starting an audit run.
/// </summary>
public class StartAuditRunRequest
{
    public Guid AuditTemplateId { get; set; }
    public string? AuditorUserId { get; set; }
}

/// <summary>
/// Request for submitting an audit answer.
/// </summary>
public class SubmitAuditAnswerRequest
{
    public Guid AuditQuestionId { get; set; }
    public int Score { get; set; }
    public string? EvidenceFileUrl { get; set; }
    public string? Comment { get; set; }
}

/// <summary>
/// Response from starting an audit run.
/// </summary>
public class StartAuditRunResponse
{
    public Guid Id { get; set; }
}

#endregion

