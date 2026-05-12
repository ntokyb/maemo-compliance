using MaemoCompliance.Application.Tenants.Dtos;
using UpdateTenantSettingsRequest = MaemoCompliance.Shared.Contracts.Engine.Tenants.UpdateTenantSettingsRequest;

namespace MaemoCompliance.Application.Engine;

/// <summary>
/// Engine interface for Tenant operations.
/// Provides a stable API surface for the Maemo Compliance Engine.
/// </summary>
public interface ITenantEngine
{
    Task<TenantDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateSettingsAsync(Guid id, UpdateTenantSettingsRequest request, CancellationToken cancellationToken = default);
}

