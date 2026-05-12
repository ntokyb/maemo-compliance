using Maemo.Application.Common;
using Maemo.Domain.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Admin.Logs;

/// <summary>
/// Handler for GetAdminErrorLogsQuery - retrieves error logs.
/// </summary>
public class GetAdminErrorLogsQueryHandler : IRequestHandler<GetAdminErrorLogsQuery, IReadOnlyList<AdminErrorLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminErrorLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminErrorLogDto>> Handle(GetAdminErrorLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ErrorLogs.AsQueryable();

        // Apply date filters
        if (request.From.HasValue)
        {
            query = query.Where(log => log.Timestamp >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(log => log.Timestamp <= request.To.Value);
        }

        // Order by timestamp descending and limit
        var logs = await query
            .OrderByDescending(log => log.Timestamp)
            .Take(request.Limit)
            .Select(log => new AdminErrorLogDto(
                log.Id,
                log.TenantId,
                log.TenantName,
                log.Message,
                log.Level,
                log.Timestamp,
                log.Source,
                log.MetadataJson
            ))
            .ToListAsync(cancellationToken);

        return logs;
    }
}

