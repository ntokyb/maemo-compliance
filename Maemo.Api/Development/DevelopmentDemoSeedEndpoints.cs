using System.Security.Claims;
using Maemo.Application.Demo;

namespace Maemo.Api.Development;

/// <summary>
/// Development-only demo seed route (auth: RequireAdmin JWT or X-Maemo-Demo-Seed-Key).
/// </summary>
public static class DevelopmentDemoSeedEndpoints
{
    public static WebApplication MapDevelopmentDemoSeedEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        var group = app.MapGroup("/admin/v1")
            .WithTags("Admin – Demo seed (Development)");

        group.MapPost("/seed-demo", async (
                HttpContext http,
                IDemoDataSeeder seeder,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("DevelopmentDemoSeed");
                var config = http.RequestServices.GetRequiredService<IConfiguration>();
                var expectedKey = config["Admin:DemoSeedApiKey"];

                var hasAdmin = http.User?.Claims.Any(c =>
                    (c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles") &&
                    (c.Value == "Admin" || c.Value == "0")) == true;

                var hasKey = !string.IsNullOrEmpty(expectedKey) &&
                    http.Request.Headers.TryGetValue("X-Maemo-Demo-Seed-Key", out var hv) &&
                    hv.Count > 0 &&
                    string.Equals(hv.ToString(), expectedKey, StringComparison.Ordinal);

                if (!hasAdmin && !hasKey)
                {
                    return Results.Unauthorized();
                }

                logger.LogInformation("Demo seed executed");

                var outcome = await seeder.SeedDemoWithOutcomeAsync(cancellationToken);

                if (outcome.WasAlreadySeeded)
                {
                    return Results.Ok(new { message = "Already seeded" });
                }

                return Results.Created(
                    "/admin/v1/seed-demo",
                    new
                    {
                        tenantId = outcome.TenantId,
                        adminEmail = outcome.AdminEmail,
                        adminPassword = "for Azure AD — create user manually",
                        message = "Demo tenant created. Create Azure AD user manually."
                    });
            })
            .WithName("Development_AdminV1_SeedDemo")
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status401Unauthorized);

        return app;
    }
}
