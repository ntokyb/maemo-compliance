using Maemo.Api.Common;
using Maemo.Application.Common;
using Maemo.Application.Demo;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Api.Portal;

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
            // Find demo tenant by default tenant ID first (for dev mode compatibility)
            var defaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var demoTenant = await context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == defaultTenantId, cancellationToken);

            // Fallback to finding by name if default ID doesn't exist
            if (demoTenant == null)
            {
                demoTenant = await context.Tenants
                    .AsNoTracking()
                    .Where(t => t.Name == "Demo Manufacturing Co.")
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (demoTenant == null)
            {
                return ErrorResults.NotFound("DemoTenantNotFound", "Demo tenant not found. Please ensure demo data has been seeded. Call POST /api/demo/seed to seed demo data.");
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

        // POST /api/demo/seed - Manually trigger demo data seeding (Development only)
        group.MapPost("/seed", async (
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken) =>
        {
            // Only allow in development
            if (!app.Environment.IsDevelopment())
            {
                return ErrorResults.BadRequest("SeedingNotAllowed", "Demo data seeding is only allowed in Development environment.");
            }

            try
            {
                using var scope = serviceProvider.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
                await seeder.SeedAsync(cancellationToken);
                
                return Results.Ok(new
                {
                    message = "Demo data seeded successfully",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("SeedingFailed", $"Failed to seed demo data: {ex.Message}");
            }
        })
        .WithName("SeedDemoData")
        .WithSummary("Seed demo data")
        .WithDescription("Manually triggers demo data seeding. Only available in Development environment.")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);
    }
}

