using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Commands;

public sealed class CreateAuditFindingCommand : IRequest<AuditFindingDto>
{
    public Guid AuditRunId { get; set; }
    public string Title { get; set; } = null!;
}
