using Maemo.Domain.Ncrs;

namespace Maemo.Application.Ncrs.Dtos;

public class UpdateNcrRequest
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public NcrSeverity Severity { get; set; }
    public DateTime? DueDate { get; set; }
    public NcrCategory Category { get; set; } = NcrCategory.Process;
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public int EscalationLevel { get; set; }
}
