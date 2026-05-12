using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Logging;

/// <summary>
/// Worker job log entry for tracking background worker executions.
/// </summary>
public class WorkerJobLog : TenantOwnedEntity
{
    public string WorkerName { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public TimeSpan Duration { get; set; }
    public string Status { get; set; } = null!; // Success, Failed, Running
    public string? ErrorMessage { get; set; }
    public string? Source { get; set; }
    public string? MetadataJson { get; set; } // Additional execution context
    public string? TenantName { get; set; } // Denormalized for easier querying
}

