namespace Maemo.Shared.Contracts.Engine.Audits;

/// <summary>
/// Request DTO for submitting an audit answer in the Engine API.
/// </summary>
public class SubmitAuditAnswerRequest
{
    public Guid AuditRunId { get; set; }
    public Guid AuditQuestionId { get; set; }
    public int Score { get; set; }
    public string? EvidenceFileUrl { get; set; }
    public string? Comment { get; set; }
}

