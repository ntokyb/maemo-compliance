using Maemo.Application.Ncrs.Dtos;
using Maemo.Domain.Ncrs;
using MediatR;

namespace Maemo.Application.Ncrs.Commands;

public class CreateNcrCommand : IRequest<Guid>
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Department { get; set; }
    public string? OwnerUserId { get; set; }
    public NcrSeverity Severity { get; set; }
    public DateTime? DueDate { get; set; }
    
    // Phase 2 enhancements
    public NcrCategory Category { get; set; } = NcrCategory.Process;
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public int EscalationLevel { get; set; } = 0;
}

