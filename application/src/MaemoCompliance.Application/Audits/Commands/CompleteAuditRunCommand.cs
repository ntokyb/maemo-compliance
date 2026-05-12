using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Commands;

public class CompleteAuditRunCommand : IRequest<AuditRunDto>
{
    public Guid AuditRunId { get; set; }
}
