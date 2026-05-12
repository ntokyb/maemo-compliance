using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Audits;

public class AuditProgramme : TenantOwnedEntity
{
    public int Year { get; set; }
    public string Title { get; set; } = null!;
    public AuditProgrammeStatus Status { get; set; } = AuditProgrammeStatus.Draft;

    public ICollection<AuditScheduleItem> Items { get; set; } = new List<AuditScheduleItem>();
}
