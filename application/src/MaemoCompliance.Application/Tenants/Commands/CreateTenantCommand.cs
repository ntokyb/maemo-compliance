using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Tenants.Commands;

public class CreateTenantCommand : IRequest<Guid>
{
    public CreateTenantRequest Request { get; set; } = null!;
}

