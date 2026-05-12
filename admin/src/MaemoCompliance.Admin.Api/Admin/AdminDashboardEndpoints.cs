using MaemoCompliance.Application.Admin.Dashboard;
using MediatR;

namespace MaemoCompliance.Admin.Api.Admin;

/// <summary>
/// Admin Dashboard API endpoints - Platform-wide metrics for Codist staff.
/// </summary>
public static class AdminDashboardEndpoints
{
    /// <summary>
    /// Maps all Admin Dashboard endpoints under the admin route group.
    /// </summary>
    public static IEndpointRouteBuilder MapAdminDashboardEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/dashboard/summary", async (ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new GetAdminDashboardSummaryQuery(), ct);
            return Results.Ok(dto);
        })
        .WithName("AdminV1_GetDashboardSummary")
        .WithSummary("Get admin dashboard summary")
        .WithDescription("Retrieves platform-wide metrics including tenant counts, API calls, errors, and failures")
        .WithOpenApi()
        .Produces<AdminDashboardSummaryDto>(StatusCodes.Status200OK);

        return group;
    }
}

