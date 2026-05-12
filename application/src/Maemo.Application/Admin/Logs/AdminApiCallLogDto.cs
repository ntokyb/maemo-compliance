namespace Maemo.Application.Admin.Logs;

/// <summary>
/// DTO for API call log entries in admin view.
/// </summary>
public sealed record AdminApiCallLogDto(
    Guid Id,
    Guid? TenantId,
    string? TenantName,
    string HttpMethod,
    string Path,
    int StatusCode,
    long DurationMs,
    DateTime Timestamp,
    string? Source
);

