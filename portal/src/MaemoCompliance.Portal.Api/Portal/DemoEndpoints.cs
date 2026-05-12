using MaemoCompliance.Portal.Api.Common;
using MaemoCompliance.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Portal.Api.Portal;

/// <summary>
/// Demo endpoints for Portal - provides quick access to demo tenant.
/// </summary>
public static class DemoEndpoints
{
    /// <summary>
    /// Maps demo endpoints under /api/demo route group.
    /// </summary>
    public static void MapDemoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/demo")
            .WithTags("Demo")
            .WithDescription("Demo mode endpoints for quick tenant access");

        // GET /api/demo/tenant - Get demo tenant information
        group.MapGet("/tenant", async (IApplicationDbContext context, CancellationToken cancellationToken) =>
        {
            // Find demo tenant by name (since Tenant doesn't have a Code property)
            var demoTenant = await context.Tenants
                .AsNoTracking()
                .Where(t => t.Name == "Demo Manufacturing Co.")
                .FirstOrDefaultAsync(cancellationToken);

            if (demoTenant == null)
            {
                return ErrorResults.NotFound("DemoTenantNotFound", "Demo tenant not found. Please ensure demo data has been seeded.");
            }

            return Results.Ok(new
            {
                tenantId = demoTenant.Id.ToString(),
                tenantName = demoTenant.Name,
                code = "DEMO", // Demo tenant code
                domain = demoTenant.Domain,
                adminEmail = demoTenant.AdminEmail,
                plan = demoTenant.Plan,
                isActive = demoTenant.IsActive
            });
        })
        .WithName("GetDemoTenant")
        .WithSummary("Get demo tenant information")
        .WithDescription("Returns the demo tenant details for quick access in demo mode")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);
    }
}

