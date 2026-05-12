using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Application.Risks.Dtos;
using MaemoCompliance.Domain.Risks;
using CreateRiskRequest = MaemoCompliance.Shared.Contracts.Engine.Risks.CreateRiskRequest;
using UpdateRiskRequest = MaemoCompliance.Shared.Contracts.Engine.Risks.UpdateRiskRequest;
using RiskFilter = MaemoCompliance.Shared.Contracts.Engine.Risks.RiskFilter;

namespace MaemoCompliance.Application.Engine;

/// <summary>
/// Engine interface for Risk Register management operations.
/// Provides a stable API surface for the Maemo Compliance Engine.
/// </summary>
public interface IRiskEngine
{
    /// <summary>
    /// Creates a new risk.
    /// </summary>
    Task<Guid> CreateAsync(CreateRiskRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing risk.
    /// </summary>
    Task UpdateAsync(Guid id, UpdateRiskRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists risks based on filter criteria.
    /// </summary>
    Task<IReadOnlyList<RiskDto>> ListAsync(RiskFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a risk by ID.
    /// </summary>
    Task<RiskDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all NCRs linked to a risk.
    /// </summary>
    Task<IReadOnlyList<NcrDto>> GetLinkedNcrsAsync(Guid riskId, CancellationToken cancellationToken = default);
}

