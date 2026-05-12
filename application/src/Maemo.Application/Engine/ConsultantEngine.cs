using Maemo.Application.Consultants.Dtos;
using Maemo.Application.Consultants.Queries;
using MediatR;

namespace Maemo.Application.Engine;

/// <summary>
/// Engine implementation for Consultant operations.
/// Acts as a facade over MediatR commands and queries.
/// </summary>
public class ConsultantEngine : IConsultantEngine
{
    private readonly IMediator _mediator;

    public ConsultantEngine(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<ConsultantDashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var query = new GetConsultantDashboardSummaryQuery();
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<IReadOnlyList<ConsultantClientDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var query = new GetConsultantClientsQuery();
        return await _mediator.Send(query, cancellationToken);
    }
}

