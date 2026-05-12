using MediatR;

namespace Maemo.Application.Audits.Commands;

public class AddAuditQuestionCommand : IRequest<Guid>
{
    public Guid AuditTemplateId { get; set; }
    public string Category { get; set; } = null!;
    public string QuestionText { get; set; } = null!;
    public int MaxScore { get; set; }
}

