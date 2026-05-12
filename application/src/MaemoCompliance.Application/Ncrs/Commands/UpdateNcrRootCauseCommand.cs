using MaemoCompliance.Application.Ncrs.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Ncrs.Commands;

public sealed class UpdateNcrRootCauseCommand : IRequest<NcrDto>
{
    public Guid NcrId { get; set; }
    public string? RootCauseMethod { get; set; }
    public string? RootCause { get; set; }
    public string? CorrectiveActionPlan { get; set; }
    public string? CorrectiveActionOwner { get; set; }
    public DateTime? CorrectiveActionDueDate { get; set; }
}
