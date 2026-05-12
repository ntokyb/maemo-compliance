using MediatR;

namespace MaemoCompliance.Application.Ncrs.Commands;

public class DeleteNcrCommand : IRequest
{
    public Guid NcrId { get; set; }
}
