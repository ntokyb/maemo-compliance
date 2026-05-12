using MediatR;

namespace MaemoCompliance.Application.Consultants.Commands;

public class RemoveConsultantFromTenantCommand : IRequest
{
    public Guid ConsultantUserId { get; set; }
    public Guid TenantId { get; set; }
}

