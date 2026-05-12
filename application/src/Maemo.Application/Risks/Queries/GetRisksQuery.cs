using Maemo.Application.Risks.Dtos;
using Maemo.Domain.Risks;
using MediatR;

namespace Maemo.Application.Risks.Queries;

public class GetRisksQuery : IRequest<IReadOnlyList<RiskDto>>
{
    public RiskCategory? Category { get; set; }
    public RiskStatus? Status { get; set; }
}

