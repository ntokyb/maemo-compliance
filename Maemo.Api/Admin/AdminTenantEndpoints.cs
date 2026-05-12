using Maemo.Api.Common;
using Maemo.Application.Admin.Tenants;
using Maemo.Application.Tenants.Commands;
using Maemo.Application.Tenants.Queries;
using MediatR;

namespace Maemo.Api.Admin;

/// <summary>
/// Request DTO for updating tenant branding.
/// </summary>
public sealed record UpdateTenantBrandingRequest(
    string? LogoUrl,
    string? PrimaryColor
);

public sealed record UpdateTenantSharePointApiRequest(
    string? SharePointSiteUrl,
    string? SharePointClientId,
    string? SharePointClientSecret,
    string? SharePointLibraryName);

public sealed record UpdateTenantLicenseApiRequest(
    string SubscriptionPlan,
    int MaxUsers,
    long MaxStorageBytes,
    DateTime? SubscriptionExpiresAt,
    string[] EnabledModules);

/// <summary>
/// Admin Tenant API endpoints - Tenant management for Codist staff.
/// </summary>
public static class AdminTenantEndpoints
{
    /// <summary>
    /// Maps all Admin Tenant endpoints under the admin route group.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminTenantEndpoints(this IEndpointRouteBuilder group)
    {
        // GET /admin/v1/tenants - List all tenants
        group.MapGet("/tenants", async (ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetAdminTenantsQuery(), ct);
            return Results.Ok(dto);
        })
        .WithName("AdminV1_GetTenants")
        .WithSummary("List all tenants")
        .WithDescription("Retrieves a list of all tenants with basic information and counts")
        .WithOpenApi()
        .Produces<IReadOnlyList<AdminTenantListItemDto>>(StatusCodes.Status200OK);

        // GET /admin/v1/tenants/{id} - Get tenant detail
        group.MapGet("/tenants/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetAdminTenantDetailQuery(id), ct);
            return dto is null ? ErrorResults.NotFound("TenantNotFound", $"Tenant with ID {id} was not found.") : Results.Ok(dto);
        })
        .WithName("AdminV1_GetTenantDetail")
        .WithSummary("Get tenant detail")
        .WithDescription("Retrieves comprehensive information about a specific tenant")
        .WithOpenApi()
        .Produces<AdminTenantDetailDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // POST /admin/v1/tenants/{id}/status - Update tenant status
        group.MapPost("/tenants/{id:guid}/status", async (
            Guid id,
            UpdateAdminTenantStatusRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new UpdateAdminTenantStatusCommand(id, request.Status), ct);
            return Results.NoContent();
        })
        .WithName("AdminV1_UpdateTenantStatus")
        .WithSummary("Update tenant status")
        .WithDescription("Updates tenant status (Active or Suspended)")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status400BadRequest);

        // GET /admin/v1/tenants/{id}/branding - Get tenant branding
        group.MapGet("/tenants/{id:guid}/branding", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantDetail = await sender.Send(new GetAdminTenantDetailQuery(id), ct);
            if (tenantDetail == null)
            {
                return ErrorResults.NotFound("TenantNotFound", $"Tenant with ID {id} was not found.");
            }

            var branding = new AdminTenantBrandingDto(
                tenantDetail.LogoUrl,
                tenantDetail.PrimaryColor
            );

            return Results.Ok(branding);
        })
        .WithName("AdminV1_GetTenantBranding")
        .WithSummary("Get tenant branding")
        .WithDescription("Retrieves branding information (logo URL and primary color) for a tenant")
        .WithOpenApi()
        .Produces<AdminTenantBrandingDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /admin/v1/tenants/{id}/branding - Update tenant branding
        group.MapPut("/tenants/{id:guid}/branding", async (
            Guid id,
            UpdateTenantBrandingRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            await sender.Send(new UpdateTenantBrandingCommand(id, request.LogoUrl, request.PrimaryColor), ct);
            return Results.NoContent();
        })
        .WithName("AdminV1_UpdateTenantBranding")
        .WithSummary("Update tenant branding")
        .WithDescription("Updates tenant branding (logo URL and primary color)")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status400BadRequest);

        // PUT /admin/v1/tenants/{id}/sharepoint
        group.MapPut("/tenants/{id:guid}/sharepoint", async (
            Guid id,
            UpdateTenantSharePointApiRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var dto = await sender.Send(new UpdateTenantSharePointCommand(
                    id,
                    request.SharePointSiteUrl,
                    request.SharePointClientId,
                    request.SharePointClientSecret,
                    request.SharePointLibraryName), ct);
                return Results.Ok(dto);
            }
            catch (KeyNotFoundException)
            {
                return ErrorResults.NotFound("TenantNotFound", $"Tenant with ID {id} was not found.");
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidSharePointConfig", ex.Message);
            }
        })
        .WithName("AdminV1_UpdateTenantSharePoint")
        .WithSummary("Update tenant SharePoint connection")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // PUT /admin/v1/tenants/{id}/license
        group.MapPut("/tenants/{id:guid}/license", async (
            Guid id,
            UpdateTenantLicenseApiRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var dto = await sender.Send(new UpdateTenantLicenseCommand(
                    id,
                    request.SubscriptionPlan,
                    request.MaxUsers,
                    request.MaxStorageBytes,
                    request.SubscriptionExpiresAt,
                    request.EnabledModules ?? Array.Empty<string>()), ct);
                return Results.Ok(dto);
            }
            catch (KeyNotFoundException)
            {
                return ErrorResults.NotFound("TenantNotFound", $"Tenant with ID {id} was not found.");
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidLicense", ex.Message);
            }
        })
        .WithName("AdminV1_UpdateTenantLicense")
        .WithSummary("Update tenant license and modules")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        // POST /admin/v1/tenants/{id}/sharepoint/test
        group.MapPost("/tenants/{id:guid}/sharepoint/test", async (
            Guid id,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new TestTenantSharePointConnectionQuery(id), ct);
                if (!result.Success)
                {
                    return Results.Json(
                        new { success = result.Success, message = result.Message, libraryUrl = result.LibraryUrl },
                        statusCode: StatusCodes.Status502BadGateway);
                }

                return Results.Ok(new { success = result.Success, message = result.Message, libraryUrl = result.LibraryUrl });
            }
            catch (KeyNotFoundException)
            {
                return ErrorResults.NotFound("TenantNotFound", $"Tenant with ID {id} was not found.");
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("SharePointTestPrecondition", ex.Message);
            }
        })
        .WithName("AdminV1_TestTenantSharePoint")
        .WithSummary("Test SharePoint / Graph connectivity for a tenant")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status502BadGateway);

        return group;
    }
}

