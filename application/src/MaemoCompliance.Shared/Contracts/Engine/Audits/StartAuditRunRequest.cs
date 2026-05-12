namespace MaemoCompliance.Shared.Contracts.Engine.Audits;

/// <summary>
/// Request DTO for starting an audit run in the Engine API.
/// </summary>
public class StartAuditRunRequest
{
    public Guid AuditTemplateId { get; set; }
    public string? AuditorUserId { get; set; }
}

