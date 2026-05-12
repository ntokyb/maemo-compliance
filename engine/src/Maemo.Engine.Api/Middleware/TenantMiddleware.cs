using Maemo.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;

namespace Maemo.Engine.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, IWebHostEnvironment environment)
    {
        // Check if user is authenticated via API Key
        // API Key authentication sets NameIdentifier claim to TenantId
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var nameIdentifierClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdentifierClaim != null && Guid.TryParse(nameIdentifierClaim.Value, out var tenantIdFromClaim))
            {
                // If authenticated via API Key, use the TenantId from the claim
                // API Key auth sets NameIdentifier to the ApiKey's TenantId
                tenantContext.TenantId = tenantIdFromClaim;
                await _next(context);
                return;
            }
        }

        // Fallback to X-Tenant-Id header (for JWT-based auth)
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            var tenantIdString = tenantIdHeader.ToString();
            if (Guid.TryParse(tenantIdString, out var tenantId))
            {
                tenantContext.TenantId = tenantId;
            }
        }
        else if (environment.IsDevelopment())
        {
            // In development, use default tenant if no header is provided
            // This allows the app to work without requiring tenant header
            tenantContext.TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        await _next(context);
    }
}

