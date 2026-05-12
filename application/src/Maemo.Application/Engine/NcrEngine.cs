using Maemo.Application.Common;
using Maemo.Application.Ncrs.Commands;
using Maemo.Application.Ncrs.Dtos;
using Maemo.Application.Ncrs.Queries;
using Maemo.Application.Risks.Dtos;
using Maemo.Application.Webhooks;
using Maemo.Domain.Ncrs;
using MediatR;

namespace Maemo.Application.Engine;

/// <summary>
/// Engine implementation for NCR management operations.
/// Acts as a facade over MediatR commands and queries.
/// </summary>
public class NcrEngine : INcrEngine
{
    private readonly IMediator _mediator;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ITenantProvider _tenantProvider;

    public NcrEngine(IMediator mediator, IWebhookDispatcher webhookDispatcher, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _webhookDispatcher = webhookDispatcher;
        _tenantProvider = tenantProvider;
    }

    public async Task<Guid> CreateAsync(CreateNcrRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateNcrCommand
        {
            Title = request.Title,
            Description = request.Description,
            Department = request.Department,
            OwnerUserId = request.OwnerUserId,
            Severity = request.Severity,
            DueDate = request.DueDate,
            Category = request.Category,
            RootCause = request.RootCause,
            CorrectiveAction = request.CorrectiveAction,
            EscalationLevel = request.EscalationLevel
        };

        var ncrId = await _mediator.Send(command, cancellationToken);

        // Dispatch webhook event
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _ = _webhookDispatcher.EnqueueAsync(tenantId, "NCR.Created", new { NcrId = ncrId, Title = request.Title }, cancellationToken);

        return ncrId;
    }

    public async Task<IReadOnlyList<NcrDto>> ListAsync(NcrFilter filter, CancellationToken cancellationToken = default)
    {
        var query = new GetNcrListQuery
        {
            Status = filter.Status,
            Severity = filter.Severity,
            Department = filter.Department
        };

        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<NcrDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetNcrByIdQuery { Id = id };
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<NcrDto> UpdateAsync(Guid id, UpdateNcrRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateNcrCommand
        {
            NcrId = id,
            Request = request
        };

        return await _mediator.Send(command, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteNcrCommand { NcrId = id };
        await _mediator.Send(command, cancellationToken);
    }

    public async Task UpdateStatusAsync(Guid id, NcrStatus status, DateTime? dueDate = null, DateTime? closedAt = null, CancellationToken cancellationToken = default)
    {
        var command = new UpdateNcrStatusCommand
        {
            Id = id,
            Status = status,
            DueDate = dueDate,
            ClosedAt = closedAt
        };

        await _mediator.Send(command, cancellationToken);

        // Dispatch webhook event
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _ = _webhookDispatcher.EnqueueAsync(tenantId, "NCR.StatusChanged", new { NcrId = id, Status = status.ToString(), DueDate = dueDate, ClosedAt = closedAt }, cancellationToken);
    }

    public async Task<IReadOnlyList<NcrStatusHistoryDto>> GetHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetNcrHistoryQuery { NcrId = id };
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task LinkToRiskAsync(Guid ncrId, Guid riskId, CancellationToken cancellationToken = default)
    {
        var command = new LinkNcrToRiskCommand
        {
            NcrId = ncrId,
            RiskId = riskId
        };

        await _mediator.Send(command, cancellationToken);
    }

    public async Task UnlinkFromRiskAsync(Guid ncrId, Guid riskId, CancellationToken cancellationToken = default)
    {
        var command = new UnlinkNcrFromRiskCommand
        {
            NcrId = ncrId,
            RiskId = riskId
        };

        await _mediator.Send(command, cancellationToken);
    }

    public async Task<IReadOnlyList<RiskDto>> GetLinkedRisksAsync(Guid ncrId, CancellationToken cancellationToken = default)
    {
        var query = new GetRisksForNcrQuery { NcrId = ncrId };
        return await _mediator.Send(query, cancellationToken);
    }
}

