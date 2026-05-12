using MediatR;

namespace Maemo.Application.Audits.Commands;

public class StartAuditRunCommand : IRequest<Guid>
{
    public Guid AuditTemplateId { get; set; }
    public string? AuditorUserId { get; set; }
}

