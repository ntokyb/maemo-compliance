namespace MaemoCompliance.Application.AuditLog.Dtos;

public class AuditLogEntryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Action { get; set; } = null!;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
}

