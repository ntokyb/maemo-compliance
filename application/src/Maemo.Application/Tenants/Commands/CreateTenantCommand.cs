using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Commands;

public class CreateTenantCommand : IRequest<Guid>
{
    public CreateTenantRequest Request { get; set; } = null!;
}

