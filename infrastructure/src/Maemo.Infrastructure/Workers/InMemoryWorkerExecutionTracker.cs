using Maemo.Application.Workers;
using System.Collections.Concurrent;

namespace Maemo.Infrastructure.Workers;

/// <summary>
/// In-memory implementation of worker execution tracker.
/// In production, this could be replaced with a database-backed implementation.
/// </summary>
public class InMemoryWorkerExecutionTracker : IWorkerExecutionTracker
{
    private readonly ConcurrentDictionary<string, List<WorkerExecutionRecord>> _executionHistory = new();
    private readonly ConcurrentDictionary<string, WorkerExecutionSummary> _latestExecutions = new();

    public void RecordExecution(string workerName, DateTime timestamp, string status, TimeSpan? duration = null, string? errorMessage = null)
    {
        var record = new WorkerExecutionRecord
        {
            WorkerName = workerName,
            Timestamp = timestamp,
            Status = status,
            Duration = duration,
            ErrorMessage = errorMessage
        };

        // Add to history
        var history = _executionHistory.GetOrAdd(workerName, _ => new List<WorkerExecutionRecord>());
        lock (history)
        {
            history.Add(record);
            // Keep only last 1000 records per worker
            if (history.Count > 1000)
            {
                history.RemoveAt(0);
            }
        }

        // Update latest execution
        var summary = _latestExecutions.GetOrAdd(workerName, _ => new WorkerExecutionSummary { WorkerName = workerName });
        summary.LastRunAt = timestamp;
        summary.LastStatus = status;
        summary.LastErrorMessage = errorMessage;
    }

    public WorkerExecutionSummary? GetLatestExecution(string workerName)
    {
        return _latestExecutions.TryGetValue(workerName, out var summary) ? summary : null;
    }

    public IReadOnlyList<WorkerExecutionRecord> GetHistory(string workerName, int limit = 50)
    {
        if (!_executionHistory.TryGetValue(workerName, out var history))
        {
            return Array.Empty<WorkerExecutionRecord>();
        }

        lock (history)
        {
            return history
                .OrderByDescending(r => r.Timestamp)
                .Take(limit)
                .ToList();
        }
    }

    public IReadOnlyList<string> GetWorkerNames()
    {
        return _latestExecutions.Keys.OrderBy(k => k).ToList();
    }
}

