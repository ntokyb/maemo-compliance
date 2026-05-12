using Maemo.Application.Audits.Dtos;
using MediatR;

namespace Maemo.Application.Audits.Queries;

public class GetAuditRunsQuery : IRequest<IReadOnlyList<AuditRunDto>>
{
}

