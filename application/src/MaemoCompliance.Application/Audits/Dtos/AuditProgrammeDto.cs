using MaemoCompliance.Domain.Audits;

namespace MaemoCompliance.Application.Audits.Dtos;

public sealed class AuditProgrammeDto
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public string Title { get; set; } = null!;
    public AuditProgrammeStatus Status { get; set; }
    public IReadOnlyList<AuditScheduleItemDto> Items { get; set; } = Array.Empty<AuditScheduleItemDto>();
}

public sealed class AuditScheduleItemDto
{
    public Guid Id { get; set; }
    public string ProcessArea { get; set; } = null!;
    public string AuditorName { get; set; } = null!;
    public DateTime PlannedDate { get; set; }
    public AuditScheduleItemStatus Status { get; set; }
    public Guid? LinkedAuditId { get; set; }
}
