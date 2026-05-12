using MediatR;

namespace Maemo.Application.Ncrs.Commands;

public class LinkNcrToRiskCommand : IRequest
{
    public Guid NcrId { get; set; }
    public Guid RiskId { get; set; }
}

