using MaemoCompliance.Application.Demo;
using Microsoft.AspNetCore.Hosting;

namespace MaemoCompliance.Api.Admin;

/// <summary>
/// Non-production admin endpoint to bootstrap demo tenant + sample data (idempotent).
/// </summary>
public static class AdminSeedEndpoints
{
    public static IEndpointRouteBuilder MapAdminSeedEndpoints(
        this IEndpointRouteBuilder endpoints,
        IWebHostEnvironment environment)
    {
        if (environment.IsProduction() || environment.IsDevelopment())
        {
            return endpoints;
        }

        var group = endpoints
            .MapGroup("/admin/v1")
            .WithTags("Admin – Seed");

        group.RequireAuthorization("RequireAdmin");

        group.MapPost("/seed-demo", async (
                IDemoDataSeeder seeder,
                CancellationToken cancellationToken) =>
            {
                await seeder.SeedAsync(cancellationToken);
                return Results.Ok(new
                {
                    seeded = true,
                    message = "Demo data seed completed (idempotent). Tenant id 11111111-1111-1111-1111-111111111111 (Demo Manufacturing Co.)."
                });
            })
            .WithName("AdminV1_SeedDemo")
            .WithSummary("Seed demo data")
            .WithDescription(
                "Runs DemoDataSeeder: ensures default demo tenant and sample documents, NCRs, risks, audit template/run. " +
                "Staging (non-Development): requires RequireAdmin. Development uses POST /admin/v1/seed-demo with API key or Admin role.")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK);

        return endpoints;
    }
}
