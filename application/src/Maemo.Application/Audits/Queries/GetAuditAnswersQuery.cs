using Maemo.Application.Audits.Dtos;
using MediatR;

namespace Maemo.Application.Audits.Queries;

public class GetAuditAnswersQuery : IRequest<IReadOnlyList<AuditAnswerDto>>
{
    public Guid AuditRunId { get; set; }
}

