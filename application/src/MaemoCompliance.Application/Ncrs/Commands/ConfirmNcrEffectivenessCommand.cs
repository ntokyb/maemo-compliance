using MediatR;

namespace MaemoCompliance.Application.Ncrs.Commands;

public sealed class ConfirmNcrEffectivenessCommand : IRequest
{
    public Guid NcrId { get; set; }
}
