using MaemoCompliance.Application.Ncrs.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Ncrs.Queries;

public class GetNcrByIdQuery : IRequest<NcrDto?>
{
    public Guid Id { get; set; }
}

