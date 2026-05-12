using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Audits;

public class AuditRun : TenantOwnedEntity
{
    public Guid AuditTemplateId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? AuditorUserId { get; set; }
}

