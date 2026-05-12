namespace MaemoCompliance.Shared.Contracts.Engine.Webhooks;

/// <summary>
/// Request DTO for creating a webhook subscription in the Engine API.
/// </summary>
public class CreateWebhookSubscriptionRequest
{
    public string Url { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string? Secret { get; set; }
}

