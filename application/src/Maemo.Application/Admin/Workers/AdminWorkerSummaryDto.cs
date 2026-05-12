namespace Maemo.Application.Admin.Workers;

/// <summary>
/// DTO for worker summary in admin view.
/// </summary>
public sealed record AdminWorkerSummaryDto(
    string Name,
    DateTime? LastRunAt,
    string LastStatus, // "Success", "Failed", "Running", "Unknown"
    DateTime? NextRunAt,
    string? LastErrorMessage
);

