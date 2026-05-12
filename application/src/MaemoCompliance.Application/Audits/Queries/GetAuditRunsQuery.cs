using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditRunsQuery : IRequest<IReadOnlyList<AuditRunDto>>
{
}

