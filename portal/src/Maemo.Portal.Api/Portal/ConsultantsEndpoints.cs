using Maemo.Application.Consultants.Commands;
using Maemo.Application.Consultants.Dtos;
using Maemo.Application.Consultants.Queries;
using Maemo.Application.Common;
using Maemo.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace Maemo.Portal.Api.Portal;

public static class ConsultantsEndpoints
{
    public static void MapConsultantsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/consultants")
            .WithTags("Consultants");
        
        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // GET /api/consultants/dashboard - Consultant only
        // Returns aggregated dashboard summary across all consultant's clients
        var dashboardRoute = group.MapGet("/dashboard", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var query = new GetConsultantDashboardSummaryQuery();
                var summary = await mediator.Send(query, cancellationToken);
                return Results.Ok(summary);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetConsultantDashboardSummary")
        .WithOpenApi()
        .Produces<ConsultantDashboardSummaryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        
        // Only require consultant authorization in production
        if (!app.Environment.IsDevelopment())
        {
            dashboardRoute.RequireAuthorization("RequireConsultant");
        }

        // GET /api/consultants/me/clients - Consultant only
        // Returns all tenants linked to logged-in consultant
        var clientsRoute = group.MapGet("/me/clients", async (
            IMediator mediator,
            ICurrentUserService currentUserService,
            CancellationToken cancellationToken) =>
        {
            // Verify user is a consultant (only in production)
            if (!app.Environment.IsDevelopment())
            {
                var userId = currentUserService.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Results.Unauthorized();
                }
            }

            try
            {
                var query = new GetConsultantClientsQuery();
                var clients = await mediator.Send(query, cancellationToken);
                return Results.Ok(clients);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetConsultantClients")
        .WithOpenApi()
        .Produces<IReadOnlyList<ConsultantClientDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        
        // Only require consultant authorization in production
        if (!app.Environment.IsDevelopment())
        {
            clientsRoute.RequireAuthorization("RequireConsultant");
        }

        // GET /api/consultants/branding - Get branding for current tenant
        // Returns tenant branding information for the consultant's current tenant context
        var brandingRoute = group.MapGet("/branding", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                // Use the tenant context to get branding
                // This assumes there's a query to get tenant branding
                // For now, return a placeholder response
                return Results.Ok(new
                {
                    message = "Branding endpoint - tenant branding will be returned here",
                    note = "This endpoint needs to be implemented with actual tenant branding query"
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GetConsultantBranding")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);
        
        // Only require consultant authorization in production
        if (!app.Environment.IsDevelopment())
        {
            brandingRoute.RequireAuthorization("RequireConsultant");
        }

        // POST /api/consultants/{consultantId}/tenants/{tenantId}
        // Admin-only: Assign consultant to tenant
        group.MapPost("/{consultantId:guid}/tenants/{tenantId:guid}", async (
            Guid consultantId,
            Guid tenantId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new AssignConsultantToTenantCommand
                {
                    ConsultantUserId = consultantId,
                    TenantId = tenantId
                };

                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization("RequireAdmin")
        .WithName("AssignConsultantToTenant")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest);

        // DELETE /api/consultants/{consultantId}/tenants/{tenantId}
        // Admin-only: Remove consultant from tenant
        group.MapDelete("/{consultantId:guid}/tenants/{tenantId:guid}", async (
            Guid consultantId,
            Guid tenantId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new RemoveConsultantFromTenantCommand
                {
                    ConsultantUserId = consultantId,
                    TenantId = tenantId
                };

                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .RequireAuthorization("RequireAdmin")
        .WithName("RemoveConsultantFromTenant")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}

