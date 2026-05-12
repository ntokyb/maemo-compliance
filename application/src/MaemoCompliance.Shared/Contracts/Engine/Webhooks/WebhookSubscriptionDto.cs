namespace MaemoCompliance.Shared.Contracts.Engine.Webhooks;

/// <summary>
/// DTO for webhook subscription information in the Engine API.
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

