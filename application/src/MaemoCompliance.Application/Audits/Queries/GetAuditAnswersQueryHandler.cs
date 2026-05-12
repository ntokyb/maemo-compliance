using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditAnswersQueryHandler : IRequestHandler<GetAuditAnswersQuery, IReadOnlyList<AuditAnswerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetAuditAnswersQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<AuditAnswerDto>> Handle(GetAuditAnswersQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify audit run exists and belongs to current tenant
        var auditRun = await _context.AuditRuns
            .FirstOrDefaultAsync(r => r.Id == request.AuditRunId && r.TenantId == tenantId, cancellationToken);

        if (auditRun == null)
        {
            throw new KeyNotFoundException($"Audit run with Id {request.AuditRunId} not found for current tenant.");
        }

        // Get all answers for this audit run, including question details
        var answers = await _context.AuditAnswers
            .Where(a => a.AuditRunId == request.AuditRunId && a.TenantId == tenantId)
            .Join(
                _context.AuditQuestions,
                answer => answer.AuditQuestionId,
                question => question.Id,
                (answer, question) => new AuditAnswerDto
                {
                    Id = answer.Id,
                    AuditRunId = answer.AuditRunId,
                    AuditQuestionId = answer.AuditQuestionId,
                    QuestionText = question.QuestionText,
                    Category = question.Category,
                    Score = answer.Score,
                    MaxScore = question.MaxScore,
                    EvidenceFileUrl = answer.EvidenceFileUrl,
                    Comment = answer.Comment
                })
            .OrderBy(a => a.Category)
            .ThenBy(a => a.QuestionText)
            .ToListAsync(cancellationToken);

        return answers;
    }
}

