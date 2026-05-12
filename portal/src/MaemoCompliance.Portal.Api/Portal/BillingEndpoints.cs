using MaemoCompliance.Portal.Api.Common;
using MaemoCompliance.Application.Billing;
using MaemoCompliance.Application.Common;

namespace MaemoCompliance.Portal.Api.Portal;

/// <summary>
/// Portal Billing API endpoints - Billing webhooks and Portal UI operations.
/// The PayFast webhook endpoint is public (no auth) as it's called by external services.
/// </summary>
public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/billing")
            .WithTags("Billing");

        // Check if billing is enabled
        var featureFlags = app.Services.GetRequiredService<IFeatureFlags>();
        if (!featureFlags.BillingEnabled)
        {
            // If billing is disabled, return 404 for all billing endpoints
            group.MapFallback(() => ErrorResults.NotFound("BillingDisabled", "Billing endpoints are disabled."));
            return;
        }

        // POST /api/billing/webhook/payfast
        // Note: Webhook endpoints typically should NOT require authorization
        // as they are called by external services (PayFast) with signature verification
        group.MapPost("/webhook/payfast", async (
            HttpContext httpContext,
            IBillingProvider billingProvider) =>
        {
            // Read raw request body
            using var reader = new StreamReader(httpContext.Request.Body);
            var payload = await reader.ReadToEndAsync();

            // Get signature from header (PayFast typically sends this in a header)
            var signature = httpContext.Request.Headers["X-PayFast-Signature"].ToString() 
                         ?? httpContext.Request.Headers["Signature"].ToString() 
                         ?? string.Empty;

            try
            {
                var handled = await billingProvider.HandleWebhookAsync(payload, signature);
                
                if (handled)
                {
                    return Results.Ok(new { status = "success", message = "Webhook processed" });
                }
                else
                {
                    return ErrorResults.BadRequest("WebhookProcessingFailed", "Webhook processing failed.");
                }
            }
            catch (Exception ex)
            {
                // Log error but return 200 to prevent PayFast from retrying
                // In production, you might want to log to a proper logging service
                return Results.Ok(new { status = "error", message = ex.Message });
            }
        })
        .AllowAnonymous() // Webhooks don't require auth
        .WithName("PayFastWebhook")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}

