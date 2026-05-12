using MaemoCompliance.Portal.Api.Common;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants.Commands;
using MaemoCompliance.Application.Tenants.Dtos;
using MaemoCompliance.Application.Tenants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaemoCompliance.Portal.Api.Portal;

/// <summary>
/// Portal Tenant API endpoints - Tenant management for Portal UI.
/// These endpoints are used by the Portal UI for tenant operations.
/// </summary>
public static class TenantsEndpoints
{
    public static void MapTenantsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenants")
            .WithTags("Tenants");
        
        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // POST /api/tenants - Admin only
        group.MapPost("/", async (
            CreateTenantRequest request,
            IMediator mediator,
            IFeatureFlags featureFlags,
            CancellationToken cancellationToken) =>
        {
            // Check if self-service signup is enabled
            if (!featureFlags.SelfServiceSignupEnabled)
            {
                return ErrorResults.BadRequest("SelfServiceSignupDisabled", "Self-service tenant creation is disabled.");
            }

            var command = new CreateTenantCommand { Request = request };
            
            try
            {
                var tenantId = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/tenants/{tenantId}", new { id = tenantId });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .RequireAuthorization("RequireAdmin")
        .WithName("CreateTenant")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status400BadRequest);

        // GET /api/tenants/{id} - Admin or TenantAdmin only
        group.MapGet("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTenantByIdQuery { Id = id };
            
            try
            {
                var tenant = await mediator.Send(query, cancellationToken);
                return Results.Ok(tenant);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization("RequireAdminOrTenantAdmin")
        .WithName("GetTenantById")
        .WithOpenApi()
        .Produces<TenantDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /api/tenants/{id} - Admin or TenantAdmin only
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTenantSettingsRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateTenantSettingsCommand
            {
                Id = id,
                Request = request
            };

            try
            {
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return ErrorResults.NotFound("TenantNotFound", $"Tenant with ID {id} was not found.");
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidOperation", ex.Message);
            }
        })
        .RequireAuthorization("RequireAdminOrTenantAdmin")
        .WithName("UpdateTenantSettings")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();

        // GET /api/tenants - Admin only
        group.MapGet("/", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetAllTenantsQuery();
            var tenants = await mediator.Send(query, cancellationToken);
            return Results.Ok(tenants);
        })
        .RequireAuthorization("RequireAdmin")
        .WithName("GetAllTenants")
        .WithOpenApi()
        .Produces<IReadOnlyList<TenantDto>>(StatusCodes.Status200OK);

        // POST /api/tenants/provision - Admin only, requires SelfServiceSignupEnabled
        group.MapPost("/provision", async (
            ProvisionTenantRequest request,
            IMediator mediator,
            IFeatureFlags featureFlags,
            CancellationToken cancellationToken) =>
        {
            // Check if self-service signup is enabled
            if (!featureFlags.SelfServiceSignupEnabled)
            {
                return ErrorResults.BadRequest("SelfServiceSignupDisabled", "Self-service tenant provisioning is disabled.");
            }

            var command = new ProvisionTenantCommand
            {
                Name = request.Name,
                AdminEmail = request.AdminEmail,
                Plan = request.Plan,
                TrialEndsAt = request.TrialEndsAt
            };

            try
            {
                var tenantId = await mediator.Send(command, cancellationToken);
                return Results.Created($"/api/tenants/{tenantId}", new { id = tenantId });
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidArgument", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidOperation", ex.Message);
            }
        })
        .RequireAuthorization("RequireAdmin")
        .WithName("ProvisionTenant")
        .WithOpenApi()
        .Produces<Guid>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status400BadRequest);

        // POST /api/tenants/{tenantId}/connect-m365 - Admin or TenantAdmin only
        group.MapPost("/{tenantId:guid}/connect-m365", async (
            Guid tenantId,
            ConnectMicrosoft365Request request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var command = new ConnectMicrosoft365Command
            {
                TenantId = tenantId,
                Request = request
            };

            try
            {
                await mediator.Send(command, cancellationToken);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return ErrorResults.NotFound("TenantNotFound", $"Tenant with ID {tenantId} was not found.");
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidArgument", ex.Message);
            }
        })
        .RequireAuthorization("RequireAdminOrTenantAdmin")
        .WithName("ConnectMicrosoft365")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesValidationProblem();
    }
}

/// <summary>
/// Request DTO for tenant provisioning.
/// </summary>
public class ProvisionTenantRequest
{
    public string Name { get; set; } = null!;
    public string AdminEmail { get; set; } = null!;
    public string Plan { get; set; } = null!;
    public DateTime? TrialEndsAt { get; set; }
}

