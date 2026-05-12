using MaemoCompliance.Application.Risks.Dtos;
using MaemoCompliance.Domain.Risks;
using MediatR;

namespace MaemoCompliance.Application.Risks.Queries;

public class GetRisksQuery : IRequest<IReadOnlyList<RiskDto>>
{
    public RiskCategory? Category { get; set; }
    public RiskStatus? Status { get; set; }
}

