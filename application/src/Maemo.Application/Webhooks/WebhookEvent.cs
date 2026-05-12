using System.Text.Json;

namespace Maemo.Application.Webhooks;

/// <summary>
/// Represents a webhook event to be dispatched to subscribers.
/// </summary>
public class WebhookEvent
{
    /// <summary>
    /// The type of event (e.g., "NCR.Created", "Document.VersionCreated").
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The event payload data.
    /// </summary>
    public JsonElement Data { get; set; }
}

