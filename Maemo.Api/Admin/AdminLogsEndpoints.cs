using Maemo.Application.Admin.Logs.Dtos;
using Maemo.Application.Admin.Logs.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Maemo.Api.Admin;

/// <summary>
/// Admin API endpoints for business audit logs.
/// </summary>
public static class AdminLogsEndpoints
{
    public static IEndpointRouteBuilder MapAdminLogsEndpoints(this IEndpointRouteBuilder group)
    {
        var logsGroup = group
            .MapGroup("/logs")
            .WithTags("Admin - Logs");

        // GET /admin/v1/logs/business
        logsGroup.MapGet("/business", async (
            IMediator mediator,
            CancellationToken cancellationToken,
            [FromQuery] Guid? tenantId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityType = null,
            [FromQuery] int limit = 100) =>
        {
            var query = new GetBusinessAuditLogsQuery
            {
                TenantId = tenantId,
                Action = action,
                EntityType = entityType,
                Limit = limit
            };

            var logs = await mediator.Send(query, cancellationToken);
            return Results.Ok(logs);
        })
        .WithName("GetBusinessAuditLogs")
        .WithOpenApi()
        .Produces<IReadOnlyList<BusinessAuditLogDto>>(StatusCodes.Status200OK);

        // GET /admin/v1/logs/business/{entityType}/{entityId}
        logsGroup.MapGet("/business/{entityType}/{entityId}", async (
            string entityType,
            string entityId,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var query = new GetEntityAuditTrailQuery
            {
                EntityType = entityType,
                EntityId = entityId
            };

            var logs = await mediator.Send(query, cancellationToken);
            return Results.Ok(logs);
        })
        .WithName("GetEntityAuditTrail")
        .WithOpenApi()
        .Produces<IReadOnlyList<BusinessAuditLogDto>>(StatusCodes.Status200OK);

        return group;
    }
}
