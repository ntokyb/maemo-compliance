using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants;
using MaemoCompliance.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Api.Middleware;

/// <summary>
/// Middleware that enforces tenant gatekeeping rules:
/// - Suspended tenants cannot access Portal or Engine endpoints
/// - ModulesEnabled controls feature access
/// </summary>
public class TenantGatekeepingMiddleware
{
    private readonly RequestDelegate _next;

    public TenantGatekeepingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        IApplicationDbContext dbContext)
    {
        // Skip gatekeeping for Admin endpoints
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/admin/v1", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Skip if no tenant context (will be handled by other middleware/guards)
        if (!tenantContext.TenantId.HasValue)
        {
            await _next(context);
            return;
        }

        var tenantId = tenantContext.TenantId.Value;

        // Load tenant to check status and modules
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        if (tenant == null)
        {
            // Tenant not found - let other middleware handle this
            await _next(context);
            return;
        }

        // Check if tenant is suspended
        if (!tenant.IsActive)
        {
            // Block Portal and Engine endpoints for suspended tenants
            if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/engine/v1/", StringComparison.OrdinalIgnoreCase))
            {
                var result = ErrorResults.Forbidden(
                    "TenantSuspended",
                    "This tenant is suspended. Please contact support."
                );
                await result.ExecuteAsync(context);
                return;
            }
            // Admin endpoints (/admin/v1/*) are NOT blocked - admin must be able to manage suspended tenants
        }

        // Store tenant info in HttpContext.Items for module checking in endpoints
        context.Items["Tenant"] = tenant;
        context.Items["TenantModules"] = tenant.GetEnabledModules();

        await _next(context);
    }
}

