using MaemoCompliance.Engine.Client.Models;

namespace MaemoCompliance.Engine.Client;

/// <summary>
/// Client interface for Risk Register operations in the Maemo Compliance Engine.
/// </summary>
public interface IRisksClient
{
    /// <summary>
    /// Retrieves a list of risks for the current tenant, optionally filtered by category and status.
    /// </summary>
    /// <param name="category">Optional risk category filter.</param>
    /// <param name="status">Optional risk status filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of risks.</returns>
    Task<IReadOnlyList<RiskDto>> GetRisksAsync(
        RiskCategory? category = null,
        RiskStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific risk by its unique identifier.
    /// </summary>
    /// <param name="id">The risk ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The risk, or null if not found.</returns>
    Task<RiskDto?> GetRiskAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new risk entry in the risk register.
    /// </summary>
    /// <param name="request">The risk creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created risk.</returns>
    Task<Guid> CreateRiskAsync(CreateRiskRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing risk.
    /// </summary>
    /// <param name="id">The risk ID.</param>
    /// <param name="request">The risk update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateRiskAsync(Guid id, UpdateRiskRequest request, CancellationToken cancellationToken = default);
}
