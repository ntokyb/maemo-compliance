using MaemoCompliance.Application.Risks.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Risks.Commands;

public class UpdateRiskCommand : IRequest
{
    public Guid Id { get; set; }
    public UpdateRiskRequest Request { get; set; } = null!;
}

