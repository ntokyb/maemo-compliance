using Maemo.Application.Ncrs.Dtos;
using MediatR;

namespace Maemo.Application.Ncrs.Queries;

public class GetNcrsForRiskQuery : IRequest<IReadOnlyList<NcrDto>>
{
    public Guid RiskId { get; set; }
}

