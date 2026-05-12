using MaemoCompliance.Application.Ncrs.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Ncrs.Queries;

public class GetNcrHistoryQuery : IRequest<IReadOnlyList<NcrStatusHistoryDto>>
{
    public Guid NcrId { get; set; }
}

