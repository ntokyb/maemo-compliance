using Maemo.Domain.Common;

namespace Maemo.Domain.AuditLog;

/// <summary>
/// Immutable audit log entry for compliance and security auditing.
/// This entity should NEVER be updated or deleted at the application level.
/// </summary>
public class AuditLogEntry : TenantOwnedEntity
{
    public string Action { get; set; } = null!;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? MetadataJson { get; set; }
}

