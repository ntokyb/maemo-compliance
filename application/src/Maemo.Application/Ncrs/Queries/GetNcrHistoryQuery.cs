using Maemo.Application.Ncrs.Dtos;
using MediatR;

namespace Maemo.Application.Ncrs.Queries;

public class GetNcrHistoryQuery : IRequest<IReadOnlyList<NcrStatusHistoryDto>>
{
    public Guid NcrId { get; set; }
}

