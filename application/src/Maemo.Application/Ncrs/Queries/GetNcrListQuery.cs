using Maemo.Application.Ncrs.Dtos;
using Maemo.Domain.Ncrs;
using MediatR;

namespace Maemo.Application.Ncrs.Queries;

public class GetNcrListQuery : IRequest<IReadOnlyList<NcrDto>>
{
    public NcrStatus? Status { get; set; }
    public NcrSeverity? Severity { get; set; }
    public string? Department { get; set; }
}

