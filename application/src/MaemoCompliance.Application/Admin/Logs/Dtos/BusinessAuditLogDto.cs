namespace MaemoCompliance.Application.Admin.Logs.Dtos;

/// <summary>
/// DTO for business audit log entries.
/// </summary>
public class BusinessAuditLogDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string UserId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string? MetadataJson { get; set; }
}

