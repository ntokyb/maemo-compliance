using MaemoCompliance.Infrastructure.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;

namespace MaemoCompliance.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, IWebHostEnvironment environment)
    {
        var path = context.Request.Path.Value ?? "";

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var isApiKey = context.User.HasClaim(c =>
                c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "ApiKeyClient");
            if (isApiKey)
            {
                var nameIdentifierClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
                if (nameIdentifierClaim != null && Guid.TryParse(nameIdentifierClaim.Value, out var tenantIdFromKey))
                {
                    tenantContext.TenantId = tenantIdFromKey;
                    await _next(context);
                    return;
                }
            }

            var tenantFromJwt = context.User.FindFirst("tenant_id");
            if (tenantFromJwt != null && Guid.TryParse(tenantFromJwt.Value, out var tidJwt))
            {
                tenantContext.TenantId = tidJwt;
                await _next(context);
                return;
            }
        }

        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
        {
            var tenantIdString = tenantIdHeader.ToString();
            if (Guid.TryParse(tenantIdString, out var tenantId))
            {
                tenantContext.TenantId = tenantId;
            }
        }
        else if (environment.IsDevelopment() && !ShouldSkipDevDefaultTenant(path))
        {
            tenantContext.TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        }

        await _next(context);
    }

    private static bool ShouldSkipDevDefaultTenant(string path) =>
        path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
        || path.StartsWith("/public", StringComparison.OrdinalIgnoreCase);
}

