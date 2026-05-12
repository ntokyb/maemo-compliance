using Maemo.Api.Common;
using Maemo.Application.Common;
using Maemo.Application.Onboarding.Commands;
using Maemo.Application.Onboarding.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Api.Portal;

public static class OnboardingEndpoints
{
    public static void MapOnboardingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/onboarding")
            .WithTags("Onboarding")
            .RequireAuthorization();

        // GET /api/onboarding/status
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
                onboardingCompletedAt = tenant.OnboardingCompletedAt
            });
        })
        .WithName("GetOnboardingStatus")
        .WithOpenApi()
        .Produces<object>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /api/onboarding/run
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
}

