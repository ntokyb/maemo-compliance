using MediatR;

namespace MaemoCompliance.Application.Audits.Commands;

public class SubmitAuditAnswerCommand : IRequest
{
    public Guid AuditRunId { get; set; }
    public Guid AuditQuestionId { get; set; }
    public int Score { get; set; }
    public string? EvidenceFileUrl { get; set; }
    public string? Comment { get; set; }
}

