using MaemoCompliance.Application.Tenants.Commands;
using MaemoCompliance.Application.Tenants.Dtos;
using MaemoCompliance.Application.Tenants.Queries;
using MediatR;
using UpdateTenantSettingsRequest = MaemoCompliance.Shared.Contracts.Engine.Tenants.UpdateTenantSettingsRequest;

namespace MaemoCompliance.Application.Engine;

/// <summary>
/// Engine implementation for Tenant operations.
/// Acts as a facade over MediatR commands and queries.
/// </summary>
public class TenantEngine : ITenantEngine
{
    private readonly IMediator _mediator;

    public TenantEngine(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<TenantDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetTenantByIdQuery { Id = id };
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task UpdateSettingsAsync(Guid id, UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateTenantSettingsCommand
        {
            Id = id,
            Request = new MaemoCompliance.Application.Tenants.Dtos.UpdateTenantSettingsRequest
            {
                Name = request.Name,
                Domain = request.Domain,
                AdminEmail = request.AdminEmail,
                IsActive = request.IsActive,
                Plan = request.Plan,
                TrialEndsAt = request.TrialEndsAt
            }
        };

        await _mediator.Send(command, cancellationToken);
    }
}

