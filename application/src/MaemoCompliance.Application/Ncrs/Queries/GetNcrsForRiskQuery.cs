using MaemoCompliance.Application.Ncrs.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Ncrs.Queries;

public class GetNcrsForRiskQuery : IRequest<IReadOnlyList<NcrDto>>
{
    public Guid RiskId { get; set; }
}

