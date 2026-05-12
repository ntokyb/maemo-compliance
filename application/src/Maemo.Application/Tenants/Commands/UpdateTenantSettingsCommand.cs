using Maemo.Application.Tenants.Dtos;
using MediatR;

namespace Maemo.Application.Tenants.Commands;

public class UpdateTenantSettingsCommand : IRequest
{
    public Guid Id { get; set; }
    public UpdateTenantSettingsRequest Request { get; set; } = null!;
}

