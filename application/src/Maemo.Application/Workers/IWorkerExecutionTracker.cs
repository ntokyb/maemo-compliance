namespace Maemo.Application.Workers;

/// <summary>
/// Service for tracking worker execution history.
/// </summary>
public interface IWorkerExecutionTracker
{
    /// <summary>
    /// Records a worker execution.
    /// </summary>
    void RecordExecution(string workerName, DateTime timestamp, string status, TimeSpan? duration = null, string? errorMessage = null);

    /// <summary>
    /// Gets the latest execution summary for a worker.
    /// </summary>
    WorkerExecutionSummary? GetLatestExecution(string workerName);

    /// <summary>
    /// Gets execution history for a worker.
    /// </summary>
    IReadOnlyList<WorkerExecutionRecord> GetHistory(string workerName, int limit = 50);

    /// <summary>
    /// Gets all known worker names.
    /// </summary>
    IReadOnlyList<string> GetWorkerNames();
}

/// <summary>
/// Summary of a worker's latest execution.
/// </summary>
public class WorkerExecutionSummary
{
    public string WorkerName { get; set; } = null!;
    public DateTime? LastRunAt { get; set; }
    public string LastStatus { get; set; } = "Unknown";
    public DateTime? NextRunAt { get; set; }
    public string? LastErrorMessage { get; set; }
}

/// <summary>
/// Record of a single worker execution.
/// </summary>
public class WorkerExecutionRecord
{
    public string WorkerName { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string Status { get; set; } = null!;
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
}

