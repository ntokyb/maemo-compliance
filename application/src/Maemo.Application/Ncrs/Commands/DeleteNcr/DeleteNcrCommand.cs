using MediatR;

namespace Maemo.Application.Ncrs.Commands;

public class DeleteNcrCommand : IRequest
{
    public Guid NcrId { get; set; }
}
