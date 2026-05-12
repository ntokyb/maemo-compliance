namespace MaemoCompliance.Shared.Contracts.Engine.Audits;

/// <summary>
/// Request DTO for adding a question to an audit template in the Engine API.
/// Note: AuditTemplateId is passed as a route parameter, not in this request.
/// </summary>
public class AddAuditQuestionRequest
{
    public string Category { get; set; } = null!;
    public string QuestionText { get; set; } = null!;
    public int MaxScore { get; set; }
}

