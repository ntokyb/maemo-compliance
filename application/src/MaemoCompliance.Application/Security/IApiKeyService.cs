using MaemoCompliance.Domain.Security;

namespace MaemoCompliance.Application.Security;

/// <summary>
/// Service for managing API keys used for engine authentication.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Validates an API key and returns the associated ApiKey entity if valid.
    /// </summary>
    Task<ApiKey?> ValidateAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new API key for a tenant.
    /// </summary>
    Task<ApiKey> CreateAsync(Guid tenantId, string? name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes (deactivates) an API key.
    /// </summary>
    Task RevokeAsync(Guid apiKeyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all API keys for a tenant.
    /// </summary>
    Task<IReadOnlyList<ApiKey>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

