namespace MaemoCompliance.Application.Common;

/// <summary>
/// Service for logging business-level audit events for compliance traceability.
/// Records semantic business events (e.g., "Document.Created", "NCR.StatusChanged").
/// </summary>
public interface IBusinessAuditLogger
{
    /// <summary>
    /// Logs a business audit event.
    /// </summary>
    /// <param name="action">The action performed (e.g., "Document.Created", "NCR.StatusChanged")</param>
    /// <param name="entityType">The type of entity (e.g., "Document", "NCR", "Risk")</param>
    /// <param name="entityId">The ID of the entity (as string)</param>
    /// <param name="metadata">Optional metadata object to serialize as JSON</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogAsync(
        string action,
        string entityType,
        string entityId,
        object? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs against an explicit tenant (e.g. anonymous signup before request tenant context exists).
    /// </summary>
    Task LogForTenantAsync(
        Guid tenantId,
        string action,
        string entityType,
        string entityId,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}

