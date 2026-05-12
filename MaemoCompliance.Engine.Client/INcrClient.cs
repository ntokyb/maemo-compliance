using MaemoCompliance.Engine.Client.Models;

namespace MaemoCompliance.Engine.Client;

/// <summary>
/// Client interface for Non-Conformance Report (NCR) operations in the Maemo Compliance Engine.
/// </summary>
public interface INcrClient
{
    /// <summary>
    /// Retrieves a list of NCRs for the current tenant, optionally filtered by status, severity, and department.
    /// </summary>
    /// <param name="status">Optional NCR status filter.</param>
    /// <param name="severity">Optional NCR severity filter.</param>
    /// <param name="department">Optional department filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of NCRs.</returns>
    Task<IReadOnlyList<NcrDto>> GetNcrsAsync(
        NcrStatus? status = null,
        NcrSeverity? severity = null,
        string? department = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific NCR by its unique identifier.
    /// </summary>
    /// <param name="id">The NCR ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The NCR, or null if not found.</returns>
    Task<NcrDto?> GetNcrAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Non-Conformance Report.
    /// </summary>
    /// <param name="request">The NCR creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created NCR.</returns>
    Task<Guid> CreateNcrAsync(CreateNcrRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the status of an NCR (e.g., Open → InProgress → Closed).
    /// </summary>
    /// <param name="id">The NCR ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="dueDate">Optional due date.</param>
    /// <param name="closedAt">Optional closed date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateNcrStatusAsync(
        Guid id,
        NcrStatus status,
        DateTime? dueDate = null,
        DateTime? closedAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the status history for an NCR.
    /// </summary>
    /// <param name="id">The NCR ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of status history entries.</returns>
    Task<IReadOnlyList<NcrStatusHistoryDto>> GetNcrHistoryAsync(Guid id, CancellationToken cancellationToken = default);
}
