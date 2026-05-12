using Maemo.Application.Common;
using Maemo.Domain.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Admin.Logs;

/// <summary>
/// Handler for GetAdminApiCallLogsQuery - retrieves API call logs.
/// </summary>
public class GetAdminApiCallLogsQueryHandler : IRequestHandler<GetAdminApiCallLogsQuery, IReadOnlyList<AdminApiCallLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminApiCallLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminApiCallLogDto>> Handle(GetAdminApiCallLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ApiCallLogs.AsQueryable();

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
            .Select(log => new AdminApiCallLogDto(
                log.Id,
                log.TenantId,
                log.TenantName,
                log.HttpMethod,
                log.Path,
                log.StatusCode,
                log.DurationMs,
                log.Timestamp,
                log.Source
            ))
            .ToListAsync(cancellationToken);

        return logs;
    }
}

