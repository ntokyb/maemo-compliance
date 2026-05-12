using MediatR;

namespace MaemoCompliance.Application.Audits.Commands;

public class CreateAuditTemplateCommand : IRequest<Guid>
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

