using Maemo.Application.Common;
using Maemo.Application.Webhooks;
using Maemo.Shared.Contracts.Engine.Webhooks;
using CreateWebhookSubscriptionRequest = Maemo.Shared.Contracts.Engine.Webhooks.CreateWebhookSubscriptionRequest;
using WebhookSubscriptionDto = Maemo.Shared.Contracts.Engine.Webhooks.WebhookSubscriptionDto;

namespace Maemo.Api.Engine.Webhooks;

/// <summary>
/// Webhooks Engine API endpoints - Webhook subscription management for the Compliance Engine.
/// </summary>
public static class WebhookEngineEndpoints
{
    /// <summary>
    /// Maps all Webhooks Engine endpoints under /engine/v1/webhooks route group.
    /// </summary>
    public static RouteGroupBuilder MapWebhookEngineEndpoints(this RouteGroupBuilder engineGroup)
    {
        var webhooksGroup = engineGroup
            .MapGroup("/webhooks")
            .WithTags("Engine - Webhooks")
            .WithDescription("Webhook subscription management - subscribe to key events and receive real-time notifications");

        // POST /engine/v1/webhooks/subscriptions
        webhooksGroup.MapPost("/subscriptions", async (
            CreateWebhookSubscriptionRequest request,
            IWebhookSubscriptionService subscriptionService,
            ITenantProvider tenantProvider,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var tenantId = tenantProvider.GetCurrentTenantId();
                var subscription = await subscriptionService.CreateAsync(
                    tenantId, 
                    request.Url, 
                    request.EventType, 
                    request.Secret, 
                    cancellationToken);

                var dto = new WebhookSubscriptionDto
                {
                    Id = subscription.Id,
                    TenantId = subscription.TenantId,
                    Url = subscription.Url,
                    EventType = subscription.EventType,
                    IsActive = subscription.IsActive,
                    CreatedAt = subscription.CreatedAt
                };

                return Results.Created($"/engine/v1/webhooks/subscriptions/{subscription.Id}", dto);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_CreateWebhookSubscription")
        .WithSummary("Create webhook subscription")
        .WithDescription(@"
Creates a new webhook subscription to receive notifications for specific events.

**Supported Event Types:**
- `NCR.Created` - When a new NCR is created
- `NCR.StatusChanged` - When an NCR status changes
- `Document.Created` - When a new document is created
- `Document.VersionCreated` - When a new document version is created
- `Risk.Created` - When a new risk is created
- `Audit.Started` - When an audit run is started

**Webhook Payload Format:**
```json
{
  ""eventType"": ""NCR.Created"",
  ""timestamp"": ""2024-01-15T10:30:00Z"",
  ""data"": { ""NcrId"": ""..."", ""Title"": ""..."" }
}
```

**Security:**
If a `secret` is provided, webhook payloads will include an `X-Maemo-Signature` header with HMAC-SHA256 signature for verification.
")
        .WithOpenApi()
        .Produces<WebhookSubscriptionDto>(StatusCodes.Status201Created, "application/json")
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // GET /engine/v1/webhooks/subscriptions
        webhooksGroup.MapGet("/subscriptions", async (
            IWebhookSubscriptionService subscriptionService,
            ITenantProvider tenantProvider,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var tenantId = tenantProvider.GetCurrentTenantId();
                var subscriptions = await subscriptionService.GetByTenantAsync(tenantId, cancellationToken);
                
                var dtos = subscriptions.Select(s => new WebhookSubscriptionDto
                {
                    Id = s.Id,
                    TenantId = s.TenantId,
                    Url = s.Url,
                    EventType = s.EventType,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                }).ToList();

                return Results.Ok(dtos);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_GetWebhookSubscriptions")
        .WithSummary("List webhook subscriptions")
        .WithDescription("Retrieves all active webhook subscriptions for the current tenant")
        .WithOpenApi()
        .Produces<IReadOnlyList<WebhookSubscriptionDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        // DELETE /engine/v1/webhooks/subscriptions/{id}
        webhooksGroup.MapDelete("/subscriptions/{id:guid}", async (
            Guid id,
            IWebhookSubscriptionService subscriptionService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await subscriptionService.DeleteAsync(id, cancellationToken);
                return Results.NoContent();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("EngineV1_DeleteWebhookSubscription")
        .WithSummary("Delete webhook subscription")
        .WithDescription("Deactivates and removes a webhook subscription. No further events will be sent to this subscription.")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest);

        return engineGroup;
    }
}

