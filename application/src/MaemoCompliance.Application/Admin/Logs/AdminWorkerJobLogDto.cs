namespace MaemoCompliance.Application.Admin.Logs;

/// <summary>
/// DTO for worker job log entries in admin view.
/// </summary>
public sealed record AdminWorkerJobLogDto(
    Guid Id,
    Guid? TenantId,
    string? TenantName,
    string WorkerName,
    DateTime Timestamp,
    TimeSpan Duration,
    string Status,
    string? ErrorMessage
);

