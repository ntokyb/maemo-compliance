using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Queries;

public sealed class GetAuditFindingByIdQuery : IRequest<AuditFindingDto?>
{
    public Guid AuditRunId { get; set; }
    public Guid FindingId { get; set; }
}
