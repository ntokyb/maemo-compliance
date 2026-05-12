using Maemo.Application.Risks.Dtos;
using MediatR;

namespace Maemo.Application.Risks.Commands;

public class CreateRiskCommand : IRequest<Guid>
{
    public CreateRiskRequest Request { get; set; } = null!;
}

