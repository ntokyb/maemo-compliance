using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Admin.Workers;

public class GetAdminWorkerHistoryQueryHandler : IRequestHandler<GetAdminWorkerHistoryQuery, IReadOnlyList<AdminWorkerJobHistoryItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminWorkerHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminWorkerJobHistoryItemDto>> Handle(GetAdminWorkerHistoryQuery request, CancellationToken cancellationToken)
    {
        var logs = await _context.WorkerJobLogs
            .AsNoTracking()
            .Where(log => log.WorkerName == request.WorkerName)
            .OrderByDescending(log => log.CreatedAt)
            .Take(request.Limit)
            .Select(log => new AdminWorkerJobHistoryItemDto(
                log.CreatedAt,
                log.Status,
                log.Duration,
                log.ErrorMessage
            ))
            .ToListAsync(cancellationToken);

        return logs;
    }
}

