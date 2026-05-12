using System.Text.Json;
using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Onboarding.Commands;
using MaemoCompliance.Application.Onboarding.Dtos;
using MaemoCompliance.Application.Tenants.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Api.Portal;

public static class OnboardingEndpoints
{
    public sealed record CompanyProfileBody(string? LogoUrl, string? Industry, string? CompanySize, string? City, string? Province);

    public sealed record TargetStandardsBody(string[] Standards);

    public sealed record InviteTeamEntry(string Email, string Role);

    public sealed record InviteTeamBody(InviteTeamEntry[] Emails);

    public static void MapOnboardingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/onboarding")
            .WithTags("Onboarding")
            .RequireAuthorization();

        group.MapGet("/status", async (
            IApplicationDbContext context,
            ITenantProvider tenantProvider,
            CancellationToken cancellationToken) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            var tenant = await context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

            if (tenant == null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new
            {
                onboardingCompleted = tenant.OnboardingCompleted,
                onboardingCompletedAt = tenant.OnboardingCompletedAt,
                setupComplete = tenant.SetupComplete,
                setupStep = tenant.SetupStep,
                tenant = new
                {
                    id = tenant.Id,
                    name = tenant.Name,
                    plan = tenant.Plan,
                    logoUrl = tenant.LogoUrl,
                    targetStandardsJson = tenant.TargetStandardsJson,
                    isActive = tenant.IsActive
                }
            });
        })
        .WithName("GetOnboardingStatus")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/company-profile", async (
            [FromBody] CompanyProfileBody body,
            IApplicationDbContext context,
            ITenantProvider tenantProvider,
            IDateTimeProvider clock,
            CancellationToken ct) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null)
            {
                return Results.NotFound();
            }

            if (!string.IsNullOrWhiteSpace(body.LogoUrl))
            {
                tenant.LogoUrl = body.LogoUrl.Trim();
            }

            tenant.Industry = string.IsNullOrWhiteSpace(body.Industry) ? tenant.Industry : body.Industry.Trim();
            tenant.CompanySize = string.IsNullOrWhiteSpace(body.CompanySize) ? tenant.CompanySize : body.CompanySize.Trim();
            tenant.City = body.City?.Trim();
            tenant.Province = body.Province?.Trim();
            tenant.SetupStep = Math.Max(tenant.SetupStep, 1);
            tenant.ModifiedAt = clock.UtcNow;
            tenant.ModifiedBy = "OnboardingWizard";

            await context.SaveChangesAsync(ct);
            return Results.Ok(new { setupStep = tenant.SetupStep });
        })
        .WithName("Onboarding_CompanyProfile")
        .WithOpenApi();

        group.MapPost("/target-standards", async (
            [FromBody] TargetStandardsBody body,
            IApplicationDbContext context,
            ITenantProvider tenantProvider,
            IDateTimeProvider clock,
            CancellationToken ct) =>
        {
            var standards = (body.Standards ?? Array.Empty<string>())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (standards.Count == 0)
            {
                return ErrorResults.BadRequest("StandardsRequired", "Select at least one standard.");
            }

            var tenantId = tenantProvider.GetCurrentTenantId();
            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null)
            {
                return Results.NotFound();
            }

            tenant.TargetStandardsJson = JsonSerializer.Serialize(standards);
            tenant.SetupStep = Math.Max(tenant.SetupStep, 2);
            tenant.ModifiedAt = clock.UtcNow;
            tenant.ModifiedBy = "OnboardingWizard";

            await context.SaveChangesAsync(ct);
            return Results.Ok(new { setupStep = tenant.SetupStep });
        })
        .WithName("Onboarding_TargetStandards")
        .WithOpenApi();

        group.MapPost("/invite-team", async (
            [FromBody] InviteTeamBody body,
            ISender sender,
            IApplicationDbContext context,
            ITenantProvider tenantProvider,
            IDateTimeProvider clock,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var baseUrl = config["App:PublicPortalUrl"] ?? "";
            var invited = 0;
            foreach (var row in body.Emails ?? Array.Empty<InviteTeamEntry>())
            {
                if (string.IsNullOrWhiteSpace(row.Email))
                {
                    continue;
                }

                var role = MapInviteRole(row.Role);
                try
                {
                    await sender.Send(new InviteUserCommand(row.Email.Trim(), role, baseUrl), ct);
                    invited++;
                }
                catch (ArgumentException)
                {
                    return ErrorResults.BadRequest("InvalidInvite", $"Invalid role or email: {row.Email}.");
                }
                catch (InvalidOperationException ex) when (ex.Message == "LICENSE_SEAT_LIMIT")
                {
                    return Results.Json(
                        new MaemoCompliance.Shared.Contracts.Common.ErrorResponse(
                            "SeatLimitReached",
                            "User limit reached for your plan.",
                            null,
                            Guid.NewGuid().ToString()),
                        statusCode: StatusCodes.Status402PaymentRequired);
                }
            }

            var tenantId = tenantProvider.GetCurrentTenantId();
            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant != null)
            {
                tenant.SetupStep = Math.Max(tenant.SetupStep, 3);
                tenant.ModifiedAt = clock.UtcNow;
                tenant.ModifiedBy = "OnboardingWizard";
                await context.SaveChangesAsync(ct);
            }

            return Results.Ok(new { invited });
        })
        .WithName("Onboarding_InviteTeam")
        .WithOpenApi();

        group.MapPost("/complete", async (
            IApplicationDbContext context,
            ITenantProvider tenantProvider,
            IDateTimeProvider clock,
            CancellationToken ct) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null)
            {
                return Results.NotFound();
            }

            tenant.SetupComplete = true;
            tenant.SetupStep = Math.Max(tenant.SetupStep, 4);
            tenant.OnboardingCompleted = true;
            tenant.OnboardingCompletedAt ??= clock.UtcNow;
            tenant.ModifiedAt = clock.UtcNow;
            tenant.ModifiedBy = "OnboardingWizard";

            await context.SaveChangesAsync(ct);

            return Results.Ok(new { redirectTo = "/app/dashboard", setupComplete = true });
        })
        .WithName("Onboarding_Complete")
        .WithOpenApi();

        group.MapPost("/run", async (
            OnboardingRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new RunOnboardingCommand { Request = request };
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidOperation", ex.Message);
            }
            catch (Exception ex)
            {
                return ErrorResults.BadRequest("OnboardingError", $"Failed to complete onboarding: {ex.Message}");
            }
        })
        .WithName("RunOnboarding")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest);
    }

    private static string MapInviteRole(string role)
    {
        var r = role.Trim();
        if (string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return "TenantAdmin";
        }

        if (string.Equals(r, "Manager", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "Viewer", StringComparison.OrdinalIgnoreCase))
        {
            return "User";
        }

        return r;
    }
}
