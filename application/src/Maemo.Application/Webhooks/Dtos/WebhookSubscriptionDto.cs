namespace Maemo.Application.Webhooks.Dtos;

/// <summary>
/// DTO for webhook subscription information.
/// </summary>
public class WebhookSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Url { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating a webhook subscription.
/// </summary>
public class CreateWebhookSubscriptionRequest
{
    public string Url { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string? Secret { get; set; }
}

