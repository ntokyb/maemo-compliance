using Maemo.Engine.Client.Models;

namespace Maemo.Engine.Client;

/// <summary>
/// Client interface for Audit management operations in the Maemo Compliance Engine.
/// </summary>
public interface IAuditClient
{
    /// <summary>
    /// Retrieves a list of audit templates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audit templates.</returns>
    Task<IReadOnlyList<AuditTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of audit runs for the current tenant.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of audit runs.</returns>
    Task<IReadOnlyList<AuditRunDto>> GetRunsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new audit run based on an audit template.
    /// </summary>
    /// <param name="auditTemplateId">The audit template ID.</param>
    /// <param name="auditorUserId">Optional auditor user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created audit run.</returns>
    Task<Guid> StartRunAsync(Guid auditTemplateId, string? auditorUserId = null, CancellationToken cancellationToken = default);
}
