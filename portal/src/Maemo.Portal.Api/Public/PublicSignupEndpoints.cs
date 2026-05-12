using Maemo.Application.Common;
using Maemo.Application.Tenants.Commands;
using Maemo.Application.Tenants.Queries;
using Maemo.Portal.Api.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Maemo.Portal.Api.Public;

public static class PublicSignupEndpoints
{
    public sealed record PublicSignupRequest(
        string CompanyName,
        string AdminEmail,
        string AdminFirstName,
        string AdminLastName,
        string Industry,
        string Plan,
        string[] IsoFrameworks);

    public static void MapPublicSignupEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/public").WithTags("Public");

        group.MapPost("/signup", async (
            HttpContext httpContext,
            [FromBody] PublicSignupRequest body,
            ISender sender,
            CancellationToken ct) =>
        {
            var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            try
            {
                var result = await sender.Send(new SignupCommand(
                    body.CompanyName,
                    body.AdminEmail,
                    body.AdminFirstName,
                    body.AdminLastName,
                    body.Industry,
                    body.Plan,
                    body.IsoFrameworks ?? Array.Empty<string>(),
                    ip), ct);

                return Results.Created($"/api/tenants/{result.TenantId}", new
                {
                    tenantId = result.TenantId,
                    message = result.Message,
                    nextStep = result.NextStep
                });
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidSignup", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "DUPLICATE_EMAIL", StringComparison.Ordinal))
                {
                    return ErrorResults.Conflict("DuplicateSignup", "An account already exists for this email.");
                }

                if (ex.Message.Contains("Too many signup", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Json(
                        new Maemo.Shared.Contracts.Common.ErrorResponse(
                            "TooManyRequests",
                            ex.Message,
                            null,
                            Guid.NewGuid().ToString()),
                        statusCode: StatusCodes.Status429TooManyRequests);
                }

                return ErrorResults.BadRequest("SignupFailed", ex.Message);
            }
        })
        .AllowAnonymous()
        .WithName("Public_Signup")
        .WithOpenApi()
        .Produces(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status429TooManyRequests);

        group.MapGet("/invite/validate", async (
            [FromQuery] string? token,
            ISender sender,
            CancellationToken ct) =>
        {
            var preview = await sender.Send(new ValidateInviteTokenQuery(token ?? ""), ct);
            return Results.Ok(preview);
        })
        .AllowAnonymous()
        .WithName("Public_ValidateInvite")
        .WithOpenApi();
    }
}
