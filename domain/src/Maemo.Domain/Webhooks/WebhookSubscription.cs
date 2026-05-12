using Maemo.Domain.Common;

namespace Maemo.Domain.Webhooks;

/// <summary>
/// Represents a webhook subscription for a tenant to receive notifications about specific events.
/// </summary>
public class WebhookSubscription : TenantOwnedEntity
{
    /// <summary>
    /// The URL where webhook events will be sent.
    /// </summary>
    public string Url { get; set; } = null!;

    /// <summary>
    /// The type of event to subscribe to (e.g., "NCR.Created", "Document.VersionCreated").
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Optional secret key for signing webhook payloads (HMAC-SHA256).
    /// </summary>
    public string? Secret { get; set; }

    /// <summary>
    /// Whether this subscription is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

