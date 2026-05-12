namespace MaemoCompliance.Application.Admin.Logs;

/// <summary>
/// DTO for error log entries in admin view.
/// </summary>
public sealed record AdminErrorLogDto(
    Guid Id,
    Guid? TenantId,
    string? TenantName,
    string Message,
    string Level,
    DateTime Timestamp,
    string? Source,
    string? MetadataJson
);

