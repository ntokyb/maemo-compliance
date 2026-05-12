using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Tenants.Commands;

public class UpdateTenantSettingsCommand : IRequest
{
    public Guid Id { get; set; }
    public UpdateTenantSettingsRequest Request { get; set; } = null!;
}

