using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Audits;

public class AuditFinding : TenantOwnedEntity
{
    public Guid AuditRunId { get; set; }
    public string Title { get; set; } = null!;
    public Guid? LinkedNcrId { get; set; }
}
