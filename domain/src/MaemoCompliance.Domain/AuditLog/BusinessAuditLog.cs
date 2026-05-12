using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.AuditLog;

/// <summary>
/// Represents a business-level audit log entry for compliance traceability.
/// Records semantic business events (e.g., "Document.Created", "NCR.StatusChanged").
/// </summary>
public class BusinessAuditLog : BaseEntity
{
    public Guid? TenantId { get; set; }
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Action { get; set; } = null!; // e.g., "Document.Created", "NCR.StatusChanged"
    public string EntityType { get; set; } = null!; // e.g., "Document", "NCR", "Risk", "AuditRun"
    public string EntityId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string? MetadataJson { get; set; } // JSON serialized metadata (diff/info/context)
}

