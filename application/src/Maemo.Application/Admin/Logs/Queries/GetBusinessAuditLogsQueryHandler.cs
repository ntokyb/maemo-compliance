using Maemo.Application.Admin.Logs.Dtos;
using Maemo.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Admin.Logs.Queries;

/// <summary>
/// Handler for retrieving business audit logs.
/// </summary>
public class GetBusinessAuditLogsQueryHandler : IRequestHandler<GetBusinessAuditLogsQuery, IReadOnlyList<BusinessAuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetBusinessAuditLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BusinessAuditLogDto>> Handle(GetBusinessAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.BusinessAuditLogs.AsQueryable();

        if (request.TenantId.HasValue)
        {
            query = query.Where(log => log.TenantId == request.TenantId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(log => log.Action == request.Action);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            query = query.Where(log => log.EntityType == request.EntityType);
        }

        var logs = await query
            .OrderByDescending(log => log.Timestamp)
            .Take(request.Limit)
            .Select(log => new BusinessAuditLogDto
            {
                Id = log.Id,
                TenantId = log.TenantId,
                UserId = log.UserId,
                Username = log.Username,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Timestamp = log.Timestamp,
                MetadataJson = log.MetadataJson
            })
            .ToListAsync(cancellationToken);

        return logs;
    }
}

