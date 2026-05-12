using MediatR;

namespace MaemoCompliance.Application.Audits.Commands;

public sealed class CreateAuditProgrammeCommand : IRequest<Guid>
{
    public int Year { get; set; }
    public string Title { get; set; } = null!;
    public IReadOnlyList<CreateAuditProgrammeItem> Items { get; set; } = Array.Empty<CreateAuditProgrammeItem>();
}

public sealed class CreateAuditProgrammeItem
{
    public string ProcessArea { get; set; } = null!;
    public string AuditorName { get; set; } = null!;
    public DateTime PlannedDate { get; set; }
}
