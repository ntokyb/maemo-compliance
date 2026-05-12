using MaemoCompliance.Domain.Ncrs;

namespace MaemoCompliance.Application.Ncrs.Dtos;

public class NcrDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public NcrSeverity Severity { get; set; }
    public NcrStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    // Phase 2 enhancements
    public NcrCategory Category { get; set; }
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public int EscalationLevel { get; set; }
    public string? RootCauseMethod { get; set; }
    public string? CorrectiveActionPlan { get; set; }
    public string? CorrectiveActionOwner { get; set; }
    public DateTime? CorrectiveActionDueDate { get; set; }
    public DateTime? CorrectiveActionCompletedAt { get; set; }
    public bool EffectivenessConfirmed { get; set; }
    public DateTime? EffectivenessVerifiedAt { get; set; }
    public Guid? LinkedAuditFindingId { get; set; }
}

public class NcrStatusHistoryDto
{
    public Guid Id { get; set; }
    public Guid NcrId { get; set; }
    public NcrStatus OldStatus { get; set; }
    public NcrStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? ChangedByUserId { get; set; }
}

