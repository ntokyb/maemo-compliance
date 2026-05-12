using Maemo.Application.Ncrs.Dtos;
using MediatR;

namespace Maemo.Application.Ncrs.Commands;

public class UpdateNcrCommand : IRequest<NcrDto>
{
    public Guid NcrId { get; set; }
    public UpdateNcrRequest Request { get; set; } = null!;
}
