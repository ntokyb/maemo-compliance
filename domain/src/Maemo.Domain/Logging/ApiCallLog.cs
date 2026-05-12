using Maemo.Domain.Common;

namespace Maemo.Domain.Logging;

/// <summary>
/// API call log entry for tracking HTTP requests and responses.
/// </summary>
public class ApiCallLog : TenantOwnedEntity
{
    public string HttpMethod { get; set; } = null!; // GET, POST, PUT, DELETE, etc.
    public string Path { get; set; } = null!; // Request path
    public int StatusCode { get; set; }
    public long DurationMs { get; set; } // Request duration in milliseconds
    public DateTime Timestamp { get; set; }
    public string? Source { get; set; } // Endpoint name or controller
    public string? MetadataJson { get; set; } // Additional request/response context
    public string? TenantName { get; set; } // Denormalized for easier querying
}

