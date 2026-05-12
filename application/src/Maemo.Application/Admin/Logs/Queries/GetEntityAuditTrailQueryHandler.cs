using Maemo.Application.Admin.Logs.Dtos;
using Maemo.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Admin.Logs.Queries;

/// <summary>
/// Handler for retrieving audit trail for a specific entity.
/// </summary>
public class GetEntityAuditTrailQueryHandler : IRequestHandler<GetEntityAuditTrailQuery, IReadOnlyList<BusinessAuditLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEntityAuditTrailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BusinessAuditLogDto>> Handle(GetEntityAuditTrailQuery request, CancellationToken cancellationToken)
    {
        var logs = await _context.BusinessAuditLogs
            .Where(log => log.EntityType == request.EntityType && log.EntityId == request.EntityId)
            .OrderByDescending(log => log.Timestamp)
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

