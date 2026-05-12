namespace MaemoCompliance.Application.Audits.Dtos;

public class AuditQuestionDto
{
    public Guid Id { get; set; }
    public Guid AuditTemplateId { get; set; }
    public string Category { get; set; } = null!;
    public string QuestionText { get; set; } = null!;
    public int MaxScore { get; set; }
}

