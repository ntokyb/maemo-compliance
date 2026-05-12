using Maemo.Domain.Common;

namespace Maemo.Domain.Logging;

/// <summary>
/// Error log entry for tracking application errors and exceptions.
/// </summary>
public class ErrorLog : TenantOwnedEntity
{
    public string Message { get; set; } = null!;
    public string Level { get; set; } = null!; // Error, Warning, Critical
    public DateTime Timestamp { get; set; }
    public string? Source { get; set; } // Controller, Service, Middleware, etc.
    public string? MetadataJson { get; set; } // Additional error context as JSON
    public string? TenantName { get; set; } // Denormalized for easier querying
}

