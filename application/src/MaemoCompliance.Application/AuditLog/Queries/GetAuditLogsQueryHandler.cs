using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.AuditLog.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.AuditLog.Queries;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, IReadOnlyList<AuditLogEntryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;

    public GetAuditLogsQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<AuditLogEntryDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        
        // Build query - start with all audit logs
        var query = _context.AuditLogs.AsQueryable();

        // Apply tenant filter - non-admin users can only see their own tenant's logs
        // System admins can specify TenantId to view other tenants' logs
        if (request.TenantId.HasValue)
        {
            // Only allow if user is system admin (check would be done at API level)
            query = query.Where(log => log.TenantId == request.TenantId.Value);
        }
        else
        {
            // Regular users see only their tenant's logs
            query = query.Where(log => log.TenantId == currentTenantId);
        }

        // Apply filters
        if (request.FromDate.HasValue)
        {
            query = query.Where(log => log.PerformedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(log => log.PerformedAt <= request.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(log => log.Action == request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(log => log.EntityType == request.EntityType);
        }

        if (request.EntityId.HasValue)
        {
            query = query.Where(log => log.EntityId == request.EntityId.Value);
        }

        // Order by most recent first
        query = query.OrderByDescending(log => log.PerformedAt);

        // Limit to prevent excessive data retrieval (adjust as needed)
        query = query.Take(10000);

        var logs = await query
            .Select(log => new AuditLogEntryDto
            {
                Id = log.Id,
                TenantId = log.TenantId,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                PerformedByUserId = log.PerformedByUserId,
                PerformedAt = log.PerformedAt,
                MetadataJson = log.MetadataJson,
                CreatedAt = log.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return logs;
    }
}

