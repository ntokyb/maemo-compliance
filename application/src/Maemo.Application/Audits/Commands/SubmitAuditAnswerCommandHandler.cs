using Maemo.Application.Common;
using Maemo.Domain.Audits;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Audits.Commands;

public class SubmitAuditAnswerCommandHandler : IRequestHandler<SubmitAuditAnswerCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public SubmitAuditAnswerCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(SubmitAuditAnswerCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify audit run exists and belongs to current tenant
        var auditRun = await _context.AuditRuns
            .FirstOrDefaultAsync(r => r.Id == command.AuditRunId && r.TenantId == tenantId, cancellationToken);

        if (auditRun == null)
        {
            throw new KeyNotFoundException($"Audit run with Id {command.AuditRunId} not found for current tenant.");
        }

        if (auditRun.CompletedAt.HasValue)
        {
            throw new ConflictException("Cannot modify a completed audit run.");
        }

        // Verify audit question exists and belongs to the template
        var question = await _context.AuditQuestions
            .FirstOrDefaultAsync(q => q.Id == command.AuditQuestionId && q.AuditTemplateId == auditRun.AuditTemplateId, cancellationToken);

        if (question == null)
        {
            throw new KeyNotFoundException($"Audit question with Id {command.AuditQuestionId} not found for this audit template.");
        }

        // Validate score
        if (command.Score < 0 || command.Score > question.MaxScore)
        {
            throw new ArgumentException($"Score must be between 0 and {question.MaxScore}.");
        }

        // Check if answer already exists
        var existingAnswer = await _context.AuditAnswers
            .FirstOrDefaultAsync(
                a => a.AuditRunId == command.AuditRunId && a.AuditQuestionId == command.AuditQuestionId,
                cancellationToken);

        if (existingAnswer != null)
        {
            // Update existing answer
            existingAnswer.Score = command.Score;
            existingAnswer.EvidenceFileUrl = command.EvidenceFileUrl;
            existingAnswer.Comment = command.Comment;
            existingAnswer.ModifiedAt = _dateTimeProvider.UtcNow;
            existingAnswer.ModifiedBy = _currentUserService.UserId;
        }
        else
        {
            // Create new answer
            var answer = new AuditAnswer
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AuditRunId = command.AuditRunId,
                AuditQuestionId = command.AuditQuestionId,
                Score = command.Score,
                EvidenceFileUrl = command.EvidenceFileUrl,
                Comment = command.Comment,
                CreatedAt = _dateTimeProvider.UtcNow,
                CreatedBy = _currentUserService.UserId
            };

            _context.AuditAnswers.Add(answer);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "AuditRun.AnswerSubmitted",
            "AuditRun",
            command.AuditRunId.ToString(),
            new { AuditQuestionId = command.AuditQuestionId, Score = command.Score, HasEvidence = !string.IsNullOrEmpty(command.EvidenceFileUrl) },
            cancellationToken);
    }
}

