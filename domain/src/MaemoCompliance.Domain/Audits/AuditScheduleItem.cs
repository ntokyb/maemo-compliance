using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Audits;

public class AuditScheduleItem : TenantOwnedEntity
{
    public Guid AuditProgrammeId { get; set; }
    public AuditProgramme AuditProgramme { get; set; } = null!;

    public string ProcessArea { get; set; } = null!;
    public string AuditorName { get; set; } = null!;
    public DateTime PlannedDate { get; set; }
    public AuditScheduleItemStatus Status { get; set; } = AuditScheduleItemStatus.Planned;
    public Guid? LinkedAuditId { get; set; }

    /// <summary>
    /// Marks the item overdue when it is still planned, no audit is linked, and the planned date is before <paramref name="utcNow"/> (date compared in UTC).
    /// </summary>
    public void ApplyOverdueIfNeeded(DateTime utcNow)
    {
        if (Status != AuditScheduleItemStatus.Planned || LinkedAuditId.HasValue)
        {
            return;
        }

        if (PlannedDate.Date < utcNow.Date)
        {
            Status = AuditScheduleItemStatus.Overdue;
        }
    }
}
