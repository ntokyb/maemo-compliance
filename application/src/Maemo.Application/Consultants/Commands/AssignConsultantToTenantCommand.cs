using MediatR;

namespace Maemo.Application.Consultants.Commands;

public class AssignConsultantToTenantCommand : IRequest
{
    public Guid ConsultantUserId { get; set; }
    public Guid TenantId { get; set; }
}

