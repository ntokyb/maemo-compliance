namespace MaemoCompliance.Application.Admin.Logs;

/// <summary>
/// DTO for webhook delivery log entries in admin view.
/// </summary>
public sealed record AdminWebhookDeliveryLogDto(
    Guid Id,
    Guid? TenantId,
    string? TenantName,
    string EventType,
    string TargetUrl,
    bool Success,
    DateTime Timestamp,
    int? StatusCode,
    string? ErrorMessage
);

