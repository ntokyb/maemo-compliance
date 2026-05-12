namespace MaemoCompliance.Application.Webhooks;

/// <summary>
/// Service for dispatching webhook events to subscribers.
/// </summary>
public interface IWebhookDispatcher
{
    /// <summary>
    /// Enqueues a webhook event to be dispatched to all active subscribers for the given tenant and event type.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="eventType">The type of event (e.g., "NCR.Created").</param>
    /// <param name="payload">The event payload data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnqueueAsync(Guid tenantId, string eventType, object payload, CancellationToken cancellationToken = default);
}

