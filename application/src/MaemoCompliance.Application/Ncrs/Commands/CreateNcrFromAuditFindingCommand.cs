using MediatR;

namespace MaemoCompliance.Application.Ncrs.Commands;

public sealed class CreateNcrFromAuditFindingCommand : IRequest<Guid>
{
    public Guid AuditRunId { get; set; }
    public Guid FindingId { get; set; }
}
