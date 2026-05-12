using Maemo.Domain.Common;

namespace Maemo.Domain.Ncrs;

public class Ncr : TenantOwnedEntity
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public NcrSeverity Severity { get; set; }
    public NcrStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    // Phase 2 enhancements
    public NcrCategory Category { get; set; } = NcrCategory.Process;
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public int EscalationLevel { get; set; } = 0; // 0 = none, 1 = supervisory, 2 = management, 3 = executive
}

