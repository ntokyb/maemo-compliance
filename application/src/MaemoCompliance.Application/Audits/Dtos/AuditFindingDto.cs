namespace MaemoCompliance.Application.Audits.Dtos;

public sealed class AuditFindingDto
{
    public Guid Id { get; set; }
    public Guid AuditRunId { get; set; }
    public string Title { get; set; } = null!;
    public Guid? LinkedNcrId { get; set; }
}
