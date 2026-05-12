using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Domain.Ncrs;
using MediatR;

namespace MaemoCompliance.Application.Ncrs.Queries;

public class GetNcrListQuery : IRequest<IReadOnlyList<NcrDto>>
{
    public NcrStatus? Status { get; set; }
    public NcrSeverity? Severity { get; set; }
    public string? Department { get; set; }
}

