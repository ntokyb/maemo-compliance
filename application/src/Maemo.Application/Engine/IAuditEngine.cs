using Maemo.Application.Audits.Commands;
using Maemo.Application.Audits.Dtos;
using CreateAuditTemplateRequest = Maemo.Shared.Contracts.Engine.Audits.CreateAuditTemplateRequest;
using AddAuditQuestionRequest = Maemo.Shared.Contracts.Engine.Audits.AddAuditQuestionRequest;
using StartAuditRunRequest = Maemo.Shared.Contracts.Engine.Audits.StartAuditRunRequest;
using SubmitAuditAnswerRequest = Maemo.Shared.Contracts.Engine.Audits.SubmitAuditAnswerRequest;

namespace Maemo.Application.Engine;

/// <summary>
/// Engine interface for Audit management operations.
/// Provides a stable API surface for the Maemo Compliance Engine.
/// </summary>
public interface IAuditEngine
{
    // Templates
    Task<IReadOnlyList<AuditTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateTemplateAsync(CreateAuditTemplateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditQuestionDto>> GetTemplateQuestionsAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<Guid> AddQuestionToTemplateAsync(Guid templateId, AddAuditQuestionRequest request, CancellationToken cancellationToken = default);

    // Runs
    Task<IReadOnlyList<AuditRunDto>> GetRunsAsync(CancellationToken cancellationToken = default);
    Task<Guid> StartRunAsync(StartAuditRunRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditAnswerDto>> GetRunAnswersAsync(Guid runId, CancellationToken cancellationToken = default);
    Task SubmitAnswerAsync(Guid runId, SubmitAuditAnswerRequest request, CancellationToken cancellationToken = default);
}

