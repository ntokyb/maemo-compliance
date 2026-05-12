using MaemoCompliance.Domain.Ncrs;
using MediatR;

namespace MaemoCompliance.Application.Ncrs.Commands;

public class UpdateNcrStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public NcrStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
}

