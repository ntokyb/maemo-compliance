using Maemo.Domain.Ncrs;
using MediatR;

namespace Maemo.Application.Ncrs.Commands;

public class UpdateNcrStatusCommand : IRequest
{
    public Guid Id { get; set; }
    public NcrStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ClosedAt { get; set; }
}

