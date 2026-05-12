using MaemoCompliance.Application.Admin.Workers;
using MediatR;

namespace MaemoCompliance.Admin.Api.Admin;

/// <summary>
/// Admin Workers endpoints - monitor worker execution status and history.
/// </summary>
public static class AdminWorkersEndpoints
{
    public static IEndpointRouteBuilder MapAdminWorkersEndpoints(this IEndpointRouteBuilder group)
    {
        // GET /admin/v1/workers - List all workers
        group.MapGet("/workers", async (ISender sender, CancellationToken ct) =>
        {
            var workers = await sender.Send(new GetAdminWorkersQuery(), ct);
            return Results.Ok(workers);
        })
        .WithName("AdminV1_GetAdminWorkers")
        .WithSummary("List all workers")
        .WithDescription("Retrieves a list of all workers with their latest execution status.")
        .WithOpenApi();

        // GET /admin/v1/workers/{name}/history - Get worker execution history
        group.MapGet("/workers/{name}/history", async (string name, int? limit, ISender sender, CancellationToken ct) =>
        {
            var historyQuery = new GetAdminWorkerHistoryQuery(name, limit ?? 50);
            var history = await sender.Send(historyQuery, ct);
            return Results.Ok(history);
        })
        .WithName("AdminV1_GetAdminWorkerHistory")
        .WithSummary("Get worker execution history")
        .WithDescription("Retrieves execution history for a specific worker.")
        .WithOpenApi();

        return group;
    }
}

