using MaemoCompliance.Application.Risks.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Risks.Queries;

public class GetRiskByIdQuery : IRequest<RiskDto?>
{
    public Guid Id { get; set; }
}

