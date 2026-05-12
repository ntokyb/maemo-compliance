namespace MaemoCompliance.Application.Admin.Workers;

/// <summary>
/// DTO for a single worker job execution history item.
/// </summary>
public sealed record AdminWorkerJobHistoryItemDto(
    DateTime Timestamp,
    string Status, // "Success", "Failed", "Running"
    TimeSpan? Duration,
    string? ErrorMessage
);

