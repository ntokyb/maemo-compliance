using Maemo.Application.Common;
using Maemo.Domain.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Admin.Logs;

/// <summary>
/// Handler for GetAdminWorkerJobLogsQuery - retrieves worker job logs.
/// </summary>
public class GetAdminWorkerJobLogsQueryHandler : IRequestHandler<GetAdminWorkerJobLogsQuery, IReadOnlyList<AdminWorkerJobLogDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminWorkerJobLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminWorkerJobLogDto>> Handle(GetAdminWorkerJobLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.WorkerJobLogs.AsQueryable();

        // Apply date filters
        if (request.From.HasValue)
        {
            query = query.Where(log => log.Timestamp >= request.From.Value);
        }

        if (request.To.HasValue)
        {
            query = query.Where(log => log.Timestamp <= request.To.Value);
        }

        // Apply failed-only filter
        if (request.FailedOnly)
        {
            query = query.Where(log => log.Status == "Failed");
        }

        // Apply worker name filter
        if (!string.IsNullOrWhiteSpace(request.WorkerName))
        {
            query = query.Where(log => log.WorkerName == request.WorkerName);
        }

        // Order by timestamp descending and limit
        var logs = await query
            .OrderByDescending(log => log.Timestamp)
            .Take(request.Limit)
            .Select(log => new AdminWorkerJobLogDto(
                log.Id,
                log.TenantId,
                log.TenantName,
                log.WorkerName,
                log.Timestamp,
                log.Duration,
                log.Status,
                log.ErrorMessage
            ))
            .ToListAsync(cancellationToken);

        return logs;
    }
}

