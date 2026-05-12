using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Workers;
using MaemoCompliance.Domain.Logging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Admin.Workers;

public class GetAdminWorkersQueryHandler : IRequestHandler<GetAdminWorkersQuery, IReadOnlyList<AdminWorkerSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IWorkerExecutionTracker _tracker;

    public GetAdminWorkersQueryHandler(IApplicationDbContext context, IWorkerExecutionTracker tracker)
    {
        _context = context;
        _tracker = tracker;
    }

    public async Task<IReadOnlyList<AdminWorkerSummaryDto>> Handle(GetAdminWorkersQuery request, CancellationToken cancellationToken)
    {
        // Get known worker names (from tracker or hardcoded list)
        var workerNames = _tracker.GetWorkerNames();
        if (workerNames.Count == 0)
        {
            workerNames = new[] { "HeartbeatWorker", "ComplianceJobsWorker" };
        }

        var results = new List<AdminWorkerSummaryDto>();

        foreach (var workerName in workerNames)
        {
            // Query database for latest execution
            var latestLog = await _context.WorkerJobLogs
                .AsNoTracking()
                .Where(log => log.WorkerName == workerName)
                .OrderByDescending(log => log.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            // Fallback to in-memory tracker if no database record
            var latestTracker = _tracker.GetLatestExecution(workerName);

            var summary = new AdminWorkerSummaryDto(
                workerName,
                latestLog?.CreatedAt ?? latestTracker?.LastRunAt,
                latestLog?.Status ?? latestTracker?.LastStatus ?? "Unknown",
                latestTracker?.NextRunAt, // NextRunAt not stored in DB
                latestLog?.ErrorMessage ?? latestTracker?.LastErrorMessage
            );

            results.Add(summary);
        }

        return results;
    }
}

