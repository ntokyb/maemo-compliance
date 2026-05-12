using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants.Commands;
using MaemoCompliance.Application.Tenants.Queries;
using MediatR;

using System.Text.Json;
using MaemoCompliance.Application.Users.Commands;
using MaemoCompliance.Application.Users.Queries;

namespace MaemoCompliance.Api.Portal;

public sealed record UpdateTenantPortalGeneralApiRequest(
    string Name,
    string? LogoUrl,
    string? PrimaryColor);

public sealed record UpdateTenantSharePointSelfApiRequest(
    string? SharePointSiteUrl,
    string? SharePointClientId,
    string? SharePointClientSecret,
    string? SharePointLibraryName);

public sealed record InviteUserApiRequest(string Email, string Role);

public sealed record AcceptInviteApiRequest(string Token);

public sealed record CompleteUserOnboardingWizardApiRequest(
    string FirstName,
    string LastName,
    string? JobTitle,
    string? Phone,
    string? OrganisationAddress,
    string? OrganisationName,
    string? LogoUrl,
    string[]? Standards,
    string[]? TeamEmails);

/// <summary>
/// Tenant self-service routes under /api/tenant (current tenant from context).
/// </summary>
public static class TenantSelfServiceEndpoints
{
    public static void MapTenantSelfServiceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/tenant").WithTags("Tenant (self-service)");

        var onboardingStatus = group.MapGet("/onboarding-status", async (ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetOnboardingStatusQuery(), ct);
            return Results.Ok(dto);
        })
        .WithName("Tenant_OnboardingStatus")
        .WithOpenApi();

        var onboardingDismiss = group.MapPost("/onboarding/dismiss", async (ISender sender, CancellationToken ct) =>
        {
            await sender.Send(new DismissOnboardingChecklistCommand(), ct);
            return Results.NoContent();
        })
        .WithName("Tenant_OnboardingDismiss")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        var acceptInvite = group.MapPost("/invitations/accept", async (
            AcceptInviteApiRequest body,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new AcceptUserInvitationCommand(body.Token), ct);
                return Results.NoContent();
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidToken", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "INVITE_INVALID", StringComparison.Ordinal))
                {
                    return ErrorResults.BadRequest("InviteInvalid", "This invitation is not valid.");
                }

                if (string.Equals(ex.Message, "INVITE_EMAIL_MISMATCH", StringComparison.Ordinal))
                {
                    return ErrorResults.Forbidden(
                        "InviteEmailMismatch",
                        "Sign in with the email address that received the invitation.");
                }

                return ErrorResults.BadRequest("AcceptFailed", ex.Message);
            }
        })
        .WithName("Tenant_AcceptInvitation")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        var directory = group.MapGet("/users", async (ISender sender, CancellationToken ct) =>
        {
            var rows = await sender.Send(new GetTenantWorkspaceDirectoryQuery(), ct);
            return Results.Ok(rows);
        })
        .WithName("Tenant_UsersDirectory")
        .WithOpenApi();

        var invite = group.MapPost("/users/invite", async (
            InviteUserApiRequest body,
            ISender sender,
            IConfiguration config,
            CancellationToken ct) =>
        {
            try
            {
                var baseUrl = config["App:PublicPortalUrl"] ?? "";
                var id = await sender.Send(
                    new InviteUserCommand(body.Email, body.Role, baseUrl),
                    ct);
                return Results.Created($"/api/tenant/users/invitations/{id}", new { id });
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidInvite", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "LICENSE_SEAT_LIMIT", StringComparison.Ordinal))
                {
                    return Results.Json(
                        new MaemoCompliance.Shared.Contracts.Common.ErrorResponse(
                            "SeatLimitReached",
                            "User limit reached for your plan. Upgrade to add more users.",
                            null,
                            Guid.NewGuid().ToString()),
                        statusCode: StatusCodes.Status402PaymentRequired);
                }

                return ErrorResults.BadRequest("InviteFailed", ex.Message);
            }
        })
        .WithName("Tenant_InviteUser")
        .WithOpenApi()
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status402PaymentRequired);

        var settings = group.MapPut("/settings", async (
            UpdateTenantPortalGeneralApiRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new UpdateTenantPortalGeneralCommand(
                    request.Name,
                    request.LogoUrl,
                    request.PrimaryColor), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return ErrorResults.NotFound("TenantNotFound", "Tenant was not found.");
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidSettings", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("InvalidSettings", ex.Message);
            }
        })
        .WithName("TenantSelf_UpdateGeneralSettings")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        var meProfile = group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetCurrentUserProfileQuery(), ct);
            return dto == null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("Tenant_MeProfile")
        .WithOpenApi();

        var completeWizard = group.MapPost("/onboarding/complete-wizard", async (
            CompleteUserOnboardingWizardApiRequest body,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(
                    new CompleteUserOnboardingWizardCommand(
                        body.FirstName,
                        body.LastName,
                        body.JobTitle,
                        body.Phone,
                        body.OrganisationAddress,
                        body.OrganisationName,
                        body.LogoUrl,
                        body.Standards ?? Array.Empty<string>(),
                        body.TeamEmails),
                    ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("OnboardingFailed", ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return ErrorResults.NotFound("UserNotFound", ex.Message);
            }
        })
        .WithName("Tenant_CompleteOnboardingWizard")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        var sharePoint = group.MapPut("/sharepoint", async (
            UpdateTenantSharePointSelfApiRequest request,
            ITenantProvider tenantProvider,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            try
            {
                var dto = await sender.Send(new UpdateTenantSharePointCommand(
                    tenantId,
                    request.SharePointSiteUrl,
                    request.SharePointClientId,
                    request.SharePointClientSecret,
                    request.SharePointLibraryName), ct);
                return Results.Ok(dto);
            }
            catch (KeyNotFoundException)
            {
                return ErrorResults.NotFound("TenantNotFound", "Tenant was not found.");
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidSharePointConfig", ex.Message);
            }
        })
        .WithName("TenantSelf_UpdateSharePoint")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        var sharePointTest = group.MapPost("/sharepoint/test", async (
            ITenantProvider tenantProvider,
            ISender sender,
            CancellationToken ct) =>
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            try
            {
                var result = await sender.Send(new TestTenantSharePointConnectionQuery(tenantId), ct);
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
                return ErrorResults.NotFound("TenantNotFound", "Tenant was not found.");
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("SharePointTestPrecondition", ex.Message);
            }
        })
        .WithName("TenantSelf_TestSharePoint")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status502BadGateway);

        if (!app.Environment.IsDevelopment())
        {
            onboardingStatus.RequireAuthorization();
            onboardingDismiss.RequireAuthorization();
            acceptInvite.RequireAuthorization();
            meProfile.RequireAuthorization();
            completeWizard.RequireAuthorization();
            directory.RequireAuthorization("RequireTenantAdmin");
            invite.RequireAuthorization("RequireTenantAdmin");
            settings.RequireAuthorization("RequireTenantAdmin");
            sharePoint.RequireAuthorization("RequireTenantAdmin");
            sharePointTest.RequireAuthorization("RequireTenantAdmin");
        }
    }
}
