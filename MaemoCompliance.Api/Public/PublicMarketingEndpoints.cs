using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.Public.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MaemoCompliance.Api.Public;

public static class PublicMarketingEndpoints
{
    public sealed record PublicContactRequest(string Name, string Company, string Email, string Message);

    public sealed record PublicAccessRequestBody(
        string CompanyName,
        string Industry,
        string CompanySize,
        string ContactName,
        string ContactEmail,
        string ContactRole,
        string[]? TargetStandards,
        string ReferralSource);

    public static void MapPublicMarketingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/public").WithTags("Public marketing");

        group.MapPost("/contact", async (
            HttpContext http,
            [FromBody] PublicContactRequest body,
            ISender sender,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Email)
                                                      || string.IsNullOrWhiteSpace(body.Message))
            {
                return ErrorResults.BadRequest("Validation", "Name, email, and message are required.");
            }

            await sender.Send(
                new SubmitPublicContactCommand(
                    body.Name.Trim(),
                    (body.Company ?? string.Empty).Trim(),
                    body.Email.Trim(),
                    body.Message.Trim()),
                ct);

            return Results.NoContent();
        })
        .AllowAnonymous()
        .WithName("Public_Contact")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/request-access", async (
            HttpContext http,
            [FromBody] PublicAccessRequestBody body,
            ISender sender,
            CancellationToken ct) =>
        {
            var ip = http.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            try
            {
                var id = await sender.Send(
                    new SubmitAccessRequestCommand(
                        body.CompanyName,
                        body.Industry,
                        body.CompanySize,
                        body.ContactName,
                        body.ContactEmail,
                        body.ContactRole,
                        body.TargetStandards ?? Array.Empty<string>(),
                        body.ReferralSource,
                        ip),
                    ct);

                return Results.Ok(new { id });
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("Validation", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(ex.Message, "DUPLICATE_PENDING_REQUEST", StringComparison.Ordinal))
                {
                    return ErrorResults.Conflict("DuplicateRequest", "A pending request already exists for this email.");
                }

                if (ex.Message.Contains("Too many requests", StringComparison.OrdinalIgnoreCase))
                {
                    return Results.Json(
                        new MaemoCompliance.Shared.Contracts.Common.ErrorResponse(
                            "TooManyRequests",
                            ex.Message,
                            null,
                            Guid.NewGuid().ToString()),
                        statusCode: StatusCodes.Status429TooManyRequests);
                }

                return ErrorResults.BadRequest("RequestFailed", ex.Message);
            }
        })
        .AllowAnonymous()
        .WithName("Public_RequestAccess")
        .WithOpenApi()
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict);
    }
}
