using Maemo.Application.AuditLog.Queries;
using Maemo.Application.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Maemo.Api.Portal;

public static class AuditLogEndpoints
{
    public static void MapAuditLogEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auditlogs")
            .WithTags("AuditLogs");

        // Require authorization - Admin or TenantAdmin only
        group.RequireAuthorization("RequireAdminOrTenantAdmin");

        // GET /api/auditlogs - Read-only endpoint for querying audit logs
        group.MapGet("/", async (
            IMediator mediator,
            ITenantProvider tenantProvider,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] Guid? tenantId = null,
            [FromQuery] string? action = null,
            [FromQuery] string? entityType = null,
            [FromQuery] Guid? entityId = null,
            CancellationToken cancellationToken = default) =>
        {
            // Only system admins can query other tenants' audit logs
            // Tenant admins can only query their own tenant's logs
            var currentTenantId = tenantProvider.GetCurrentTenantId();
            if (tenantId.HasValue && tenantId.Value != currentTenantId)
            {
                // TODO: Check if user is system admin
                // For now, only allow querying own tenant
                return Results.Forbid();
            }

            var query = new GetAuditLogsQuery
            {
                FromDate = fromDate,
                ToDate = toDate,
                TenantId = tenantId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId
            };

            var logs = await mediator.Send(query, cancellationToken);
            return Results.Ok(logs);
        })
        .WithName("GetAuditLogs")
        .WithOpenApi()
        .Produces<IReadOnlyList<Maemo.Application.AuditLog.Dtos.AuditLogEntryDto>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden);
    }
}

