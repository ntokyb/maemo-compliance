using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Logging;

/// <summary>
/// Webhook delivery log entry for tracking webhook event deliveries.
/// </summary>
public class WebhookDeliveryLog : TenantOwnedEntity
{
    public string EventType { get; set; } = null!; // Document.Created, NCR.StatusChanged, etc.
    public string TargetUrl { get; set; } = null!;
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
    public int? StatusCode { get; set; } // HTTP status code from webhook delivery
    public string? ErrorMessage { get; set; }
    public string? Source { get; set; }
    public string? MetadataJson { get; set; } // Additional delivery context
    public string? TenantName { get; set; } // Denormalized for easier querying
}

