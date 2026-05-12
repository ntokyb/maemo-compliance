using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditAnswersQuery : IRequest<IReadOnlyList<AuditAnswerDto>>
{
    public Guid AuditRunId { get; set; }
}

