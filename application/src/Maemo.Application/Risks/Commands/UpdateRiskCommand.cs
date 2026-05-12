using Maemo.Application.Risks.Dtos;
using MediatR;

namespace Maemo.Application.Risks.Commands;

public class UpdateRiskCommand : IRequest
{
    public Guid Id { get; set; }
    public UpdateRiskRequest Request { get; set; } = null!;
}

