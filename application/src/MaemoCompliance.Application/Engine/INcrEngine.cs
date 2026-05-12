using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Application.Risks.Dtos;
using MaemoCompliance.Domain.Ncrs;

namespace MaemoCompliance.Application.Engine;

/// <summary>
/// Engine interface for NCR (Non-Conformance Report) management operations.
/// Provides a stable API surface for the Maemo Compliance Engine.
/// </summary>
public interface INcrEngine
{
    /// <summary>
    /// Creates a new NCR.
    /// </summary>
    Task<Guid> CreateAsync(CreateNcrRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists NCRs based on filter criteria.
    /// </summary>
    Task<IReadOnlyList<NcrDto>> ListAsync(NcrFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an NCR by ID.
    /// </summary>
    Task<NcrDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates editable fields of an NCR (not status — use <see cref="UpdateStatusAsync"/>).
    /// </summary>
    Task<NcrDto> UpdateAsync(Guid id, UpdateNcrRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an NCR and its risk links and status history.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an NCR.
    /// </summary>
    Task UpdateStatusAsync(Guid id, NcrStatus status, DateTime? dueDate = null, DateTime? closedAt = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the history of status changes for an NCR.
    /// </summary>
    Task<IReadOnlyList<NcrStatusHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links an NCR to a Risk.
    /// </summary>
    Task LinkToRiskAsync(Guid ncrId, Guid riskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlinks an NCR from a Risk.
    /// </summary>
    Task UnlinkFromRiskAsync(Guid ncrId, Guid riskId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all Risks linked to an NCR.
    /// </summary>
    Task<IReadOnlyList<RiskDto>> GetLinkedRisksAsync(Guid ncrId, CancellationToken cancellationToken = default);
}

