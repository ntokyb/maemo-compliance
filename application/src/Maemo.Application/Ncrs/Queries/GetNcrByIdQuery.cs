using Maemo.Application.Ncrs.Dtos;
using MediatR;

namespace Maemo.Application.Ncrs.Queries;

public class GetNcrByIdQuery : IRequest<NcrDto?>
{
    public Guid Id { get; set; }
}

