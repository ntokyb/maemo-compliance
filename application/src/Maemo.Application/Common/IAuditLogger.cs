namespace Maemo.Application.Common;

/// <summary>
/// Interface for immutable audit logging.
/// All audit log entries are append-only and cannot be modified or deleted.
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string? entityType = null,
        Guid? entityId = null,
        object? metadata = null,
        CancellationToken cancellationToken = default);
}

