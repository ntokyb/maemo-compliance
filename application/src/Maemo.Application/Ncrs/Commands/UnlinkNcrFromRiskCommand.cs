using MediatR;

namespace Maemo.Application.Ncrs.Commands;

public class UnlinkNcrFromRiskCommand : IRequest
{
    public Guid NcrId { get; set; }
    public Guid RiskId { get; set; }
}

