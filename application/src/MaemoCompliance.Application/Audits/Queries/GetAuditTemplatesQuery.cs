using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditTemplatesQuery : IRequest<IReadOnlyList<AuditTemplateDto>>
{
}

