using Maemo.Application.Common;
using Maemo.Application.Webhooks;
using Maemo.Domain.Webhooks;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Maemo.Infrastructure.Webhooks;

/// <summary>
/// Simple implementation of webhook dispatcher that sends events synchronously.
/// For production, consider using a background queue service.
/// </summary>
public class WebhookDispatcher : IWebhookDispatcher
{
    private readonly IWebhookSubscriptionService _subscriptionService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        IWebhookSubscriptionService subscriptionService,
        IHttpClientFactory httpClientFactory,
        IDateTimeProvider dateTimeProvider,
        ILogger<WebhookDispatcher> logger)
    {
        _subscriptionService = subscriptionService;
        _httpClientFactory = httpClientFactory;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task EnqueueAsync(Guid tenantId, string eventType, object payload, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all active subscriptions for this tenant and event type
            var subscriptions = await _subscriptionService.GetByTenantAndEventTypeAsync(tenantId, eventType, cancellationToken);

            if (!subscriptions.Any())
            {
                _logger.LogDebug("No active webhook subscriptions found for tenant {TenantId} and event type {EventType}", tenantId, eventType);
                return;
            }

            // Create webhook event
            var webhookEvent = new WebhookEvent
            {
                EventType = eventType,
                Timestamp = _dateTimeProvider.UtcNow,
                Data = JsonSerializer.SerializeToElement(payload)
            };

            // Dispatch to all subscribers
            var tasks = subscriptions.Select(subscription => DispatchToSubscriberAsync(subscription, webhookEvent, cancellationToken));
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching webhook event {EventType} for tenant {TenantId}", eventType, tenantId);
            // Don't throw - webhook failures shouldn't break the main operation
        }
    }

    private async Task DispatchToSubscriberAsync(WebhookSubscription subscription, WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30); // 30 second timeout

            var jsonContent = JsonSerializer.Serialize(webhookEvent);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Add signature header if secret is present
            if (!string.IsNullOrWhiteSpace(subscription.Secret))
            {
                var signature = ComputeSignature(jsonContent, subscription.Secret);
                content.Headers.Add("X-Maemo-Signature", signature);
            }

            var response = await httpClient.PostAsync(subscription.Url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook dispatched successfully to {Url} for event {EventType}", subscription.Url, webhookEvent.EventType);
            }
            else
            {
                _logger.LogWarning("Webhook dispatch failed to {Url} for event {EventType}. Status: {StatusCode}", 
                    subscription.Url, webhookEvent.EventType, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching webhook to {Url} for event {EventType}", subscription.Url, webhookEvent.EventType);
            // Don't throw - continue with other subscribers
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        // Convert byte array to hex string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

