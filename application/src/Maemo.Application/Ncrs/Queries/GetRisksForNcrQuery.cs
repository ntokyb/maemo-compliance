using Maemo.Application.Risks.Dtos;
using MediatR;

namespace Maemo.Application.Ncrs.Queries;

public class GetRisksForNcrQuery : IRequest<IReadOnlyList<RiskDto>>
{
    public Guid NcrId { get; set; }
}

