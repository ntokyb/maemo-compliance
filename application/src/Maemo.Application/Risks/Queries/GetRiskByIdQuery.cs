using Maemo.Application.Risks.Dtos;
using MediatR;

namespace Maemo.Application.Risks.Queries;

public class GetRiskByIdQuery : IRequest<RiskDto?>
{
    public Guid Id { get; set; }
}

