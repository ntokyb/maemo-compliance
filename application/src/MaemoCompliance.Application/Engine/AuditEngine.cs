using MaemoCompliance.Application.Audits.Commands;
using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Audits.Queries;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Webhooks;
using MediatR;
using CreateAuditTemplateRequest = MaemoCompliance.Shared.Contracts.Engine.Audits.CreateAuditTemplateRequest;
using AddAuditQuestionRequest = MaemoCompliance.Shared.Contracts.Engine.Audits.AddAuditQuestionRequest;
using StartAuditRunRequest = MaemoCompliance.Shared.Contracts.Engine.Audits.StartAuditRunRequest;
using SubmitAuditAnswerRequest = MaemoCompliance.Shared.Contracts.Engine.Audits.SubmitAuditAnswerRequest;

namespace MaemoCompliance.Application.Engine;

/// <summary>
/// Engine implementation for Audit management operations.
/// Acts as a facade over MediatR commands and queries.
/// </summary>
public class AuditEngine : IAuditEngine
{
    private readonly IMediator _mediator;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ITenantProvider _tenantProvider;

    public AuditEngine(IMediator mediator, IWebhookDispatcher webhookDispatcher, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _webhookDispatcher = webhookDispatcher;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<AuditTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var query = new GetAuditTemplatesQuery();
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<Guid> CreateTemplateAsync(CreateAuditTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateAuditTemplateCommand
        {
            Name = request.Name,
            Description = request.Description
        };
        return await _mediator.Send(command, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditQuestionDto>> GetTemplateQuestionsAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        var query = new GetAuditQuestionsQuery { AuditTemplateId = templateId };
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<Guid> AddQuestionToTemplateAsync(Guid templateId, AddAuditQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new AddAuditQuestionCommand
        {
            AuditTemplateId = templateId,
            Category = request.Category,
            QuestionText = request.QuestionText,
            MaxScore = request.MaxScore
        };
        return await _mediator.Send(command, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditRunDto>> GetRunsAsync(CancellationToken cancellationToken = default)
    {
        var query = new GetAuditRunsQuery();
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<Guid> StartRunAsync(StartAuditRunRequest request, CancellationToken cancellationToken = default)
    {
        var command = new StartAuditRunCommand
        {
            AuditTemplateId = request.AuditTemplateId,
            AuditorUserId = request.AuditorUserId
        };
        var runId = await _mediator.Send(command, cancellationToken);

        // Dispatch webhook event
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _ = _webhookDispatcher.EnqueueAsync(tenantId, "Audit.Started", new { AuditRunId = runId, AuditTemplateId = request.AuditTemplateId }, cancellationToken);

        return runId;
    }

    public async Task<IReadOnlyList<AuditAnswerDto>> GetRunAnswersAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var query = new GetAuditAnswersQuery { AuditRunId = runId };
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task SubmitAnswerAsync(Guid runId, SubmitAuditAnswerRequest request, CancellationToken cancellationToken = default)
    {
        var command = new SubmitAuditAnswerCommand
        {
            AuditRunId = runId,
            AuditQuestionId = request.AuditQuestionId,
            Score = request.Score,
            EvidenceFileUrl = request.EvidenceFileUrl,
            Comment = request.Comment
        };
        await _mediator.Send(command, cancellationToken);
    }
}

