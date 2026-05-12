using MaemoCompliance.Application.Risks.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Risks.Commands;

public class CreateRiskCommand : IRequest<Guid>
{
    public CreateRiskRequest Request { get; set; } = null!;
}

