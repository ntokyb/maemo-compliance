using MaemoCompliance.Api.Common;
using MaemoCompliance.Application.AccessRequests.Commands;
using MaemoCompliance.Application.AccessRequests.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MaemoCompliance.Api.Admin;

public static class AdminAccessRequestEndpoints
{
    public sealed record ApproveAccessRequestBody(string CompanyName, string Plan);

    public sealed record RejectAccessRequestBody(string? Reason);

    public static IEndpointRouteBuilder MapAdminAccessRequestEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/access-requests/pending-count", async (ISender sender, CancellationToken ct) =>
        {
            var n = await sender.Send(new GetPendingAccessRequestCountQuery(), ct);
            return Results.Ok(new { pending = n });
        })
        .WithName("AdminV1_AccessRequestsPendingCount")
        .WithOpenApi();

        group.MapGet("/access-requests", async (
            [FromQuery] string? status,
            ISender sender,
            CancellationToken ct) =>
        {
            var rows = await sender.Send(new GetAccessRequestsQuery(status), ct);
            return Results.Ok(rows);
        })
        .WithName("AdminV1_ListAccessRequests")
        .WithOpenApi();

        group.MapPost("/access-requests/{id:guid}/approve", async (
            Guid id,
            [FromBody] ApproveAccessRequestBody body,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new ApproveAccessRequestCommand(id, body.CompanyName, body.Plan), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (ArgumentException ex)
            {
                return ErrorResults.BadRequest("InvalidPlan", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("ApproveFailed", ex.Message);
            }
        })
        .WithName("AdminV1_ApproveAccessRequest")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/access-requests/{id:guid}/reject", async (
            Guid id,
            [FromBody] RejectAccessRequestBody body,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                await sender.Send(new RejectAccessRequestCommand(id, body.Reason), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResults.BadRequest("RejectFailed", ex.Message);
            }
        })
        .WithName("AdminV1_RejectAccessRequest")
        .WithOpenApi()
        .Produces(StatusCodes.Status204NoContent);

        return group;
    }
}
