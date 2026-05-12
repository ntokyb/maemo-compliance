using MaemoCompliance.Domain.Webhooks;

namespace MaemoCompliance.Application.Webhooks;

/// <summary>
/// Service for managing webhook subscriptions.
/// </summary>
public interface IWebhookSubscriptionService
{
    /// <summary>
    /// Creates a new webhook subscription.
    /// </summary>
    Task<WebhookSubscription> CreateAsync(Guid tenantId, string url, string eventType, string? secret, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active subscriptions for a tenant.
    /// </summary>
    Task<IReadOnlyList<WebhookSubscription>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active subscriptions for a tenant and event type.
    /// </summary>
    Task<IReadOnlyList<WebhookSubscription>> GetByTenantAndEventTypeAsync(Guid tenantId, string eventType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates or removes a webhook subscription.
    /// </summary>
    Task DeleteAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
}

