namespace Maemo.Application.Audits.Dtos;

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

