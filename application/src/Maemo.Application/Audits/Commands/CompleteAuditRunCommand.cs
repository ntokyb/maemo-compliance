using Maemo.Application.Audits.Dtos;
using MediatR;

namespace Maemo.Application.Audits.Commands;

public class CompleteAuditRunCommand : IRequest<AuditRunDto>
{
    public Guid AuditRunId { get; set; }
}
