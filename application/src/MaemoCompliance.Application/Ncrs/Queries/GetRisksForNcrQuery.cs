using MaemoCompliance.Application.Risks.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Ncrs.Queries;

public class GetRisksForNcrQuery : IRequest<IReadOnlyList<RiskDto>>
{
    public Guid NcrId { get; set; }
}

