using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Ncrs.Dtos;
using MaemoCompliance.Application.Ncrs.Queries;
using MaemoCompliance.Application.Risks.Commands;
using MaemoCompliance.Application.Risks.Dtos;
using MaemoCompliance.Application.Risks.Queries;
using MaemoCompliance.Application.Webhooks;
using MediatR;
using CreateRiskRequest = MaemoCompliance.Shared.Contracts.Engine.Risks.CreateRiskRequest;
using UpdateRiskRequest = MaemoCompliance.Shared.Contracts.Engine.Risks.UpdateRiskRequest;
using RiskFilter = MaemoCompliance.Shared.Contracts.Engine.Risks.RiskFilter;

namespace MaemoCompliance.Application.Engine;

/// <summary>
/// Engine implementation for Risk Register management operations.
/// Acts as a facade over MediatR commands and queries.
/// </summary>
public class RiskEngine : IRiskEngine
{
    private readonly IMediator _mediator;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ITenantProvider _tenantProvider;

    public RiskEngine(IMediator mediator, IWebhookDispatcher webhookDispatcher, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _webhookDispatcher = webhookDispatcher;
        _tenantProvider = tenantProvider;
    }

    public async Task<Guid> CreateAsync(CreateRiskRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateRiskCommand
        {
            Request = new MaemoCompliance.Application.Risks.Dtos.CreateRiskRequest
            {
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Cause = request.Cause,
                Consequences = request.Consequences,
                InherentLikelihood = request.InherentLikelihood,
                InherentImpact = request.InherentImpact,
                ExistingControls = request.ExistingControls,
                ResidualLikelihood = request.ResidualLikelihood,
                ResidualImpact = request.ResidualImpact,
                OwnerUserId = request.OwnerUserId,
                Status = request.Status
            }
        };

        var riskId = await _mediator.Send(command, cancellationToken);

        // Dispatch webhook event
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _ = _webhookDispatcher.EnqueueAsync(tenantId, "Risk.Created", new { RiskId = riskId, Title = request.Title }, cancellationToken);

        return riskId;
    }

    public async Task UpdateAsync(Guid id, UpdateRiskRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateRiskCommand
        {
            Id = id,
            Request = new MaemoCompliance.Application.Risks.Dtos.UpdateRiskRequest
            {
                Title = request.Title,
                Description = request.Description,
                Category = request.Category,
                Cause = request.Cause,
                Consequences = request.Consequences,
                InherentLikelihood = request.InherentLikelihood,
                InherentImpact = request.InherentImpact,
                ExistingControls = request.ExistingControls,
                ResidualLikelihood = request.ResidualLikelihood,
                ResidualImpact = request.ResidualImpact,
                OwnerUserId = request.OwnerUserId,
                Status = request.Status
            }
        };

        await _mediator.Send(command, cancellationToken);
    }

    public async Task<IReadOnlyList<RiskDto>> ListAsync(RiskFilter filter, CancellationToken cancellationToken = default)
    {
        var query = new GetRisksQuery
        {
            Category = filter.Category,
            Status = filter.Status
        };

        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<RiskDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetRiskByIdQuery { Id = id };
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<IReadOnlyList<NcrDto>> GetLinkedNcrsAsync(Guid riskId, CancellationToken cancellationToken = default)
    {
        var query = new GetNcrsForRiskQuery { RiskId = riskId };
        return await _mediator.Send(query, cancellationToken);
    }
}

