using MediatR;

namespace MaemoCompliance.Application.Audits.Commands;

public sealed class LinkAuditToScheduleItemCommand : IRequest
{
    public Guid ProgrammeId { get; set; }
    public Guid ItemId { get; set; }
    public Guid AuditId { get; set; }
}
