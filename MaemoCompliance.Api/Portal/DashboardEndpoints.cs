using MaemoCompliance.Application.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace MaemoCompliance.Api.Portal;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard");
        
        // Only require authorization in production
        if (!app.Environment.IsDevelopment())
        {
            group.RequireAuthorization();
        }

        // GET /api/dashboard/summary
        group.MapGet("/summary", async (
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDashboardSummaryQuery();
            var summary = await mediator.Send(query, cancellationToken);
            return Results.Ok(summary);
        })
        .WithName("GetDashboardSummary")
        .WithOpenApi()
        .Produces<DashboardSummaryDto>(StatusCodes.Status200OK);
    }
}

