using Maemo.Application.Audits.Dtos;
using Maemo.Application.Common;
using Maemo.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Audits.Queries;

public class GetAuditQuestionsQueryHandler : IRequestHandler<GetAuditQuestionsQuery, IReadOnlyList<AuditQuestionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAuditQuestionsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<AuditQuestionDto>> Handle(GetAuditQuestionsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userId = Guid.Parse(currentUserId);

        // Verify template exists
        var template = await _context.AuditTemplates
            .FirstOrDefaultAsync(t => t.Id == request.AuditTemplateId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Audit template with Id {request.AuditTemplateId} not found.");
        }

        // Verify user is a consultant and owns the template (or allow tenant users to view questions for audit runs)
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        // Consultants can view their own templates, tenant users can view any template (for audit runs)
        if (user.Role == UserRole.Consultant && template.ConsultantUserId != userId)
        {
            throw new UnauthorizedAccessException("You can only view questions for your own audit templates.");
        }

        var questions = await _context.AuditQuestions
            .Where(q => q.AuditTemplateId == request.AuditTemplateId)
            .OrderBy(q => q.Category)
            .ThenBy(q => q.CreatedAt)
            .Select(q => new AuditQuestionDto
            {
                Id = q.Id,
                AuditTemplateId = q.AuditTemplateId,
                Category = q.Category,
                QuestionText = q.QuestionText,
                MaxScore = q.MaxScore
            })
            .ToListAsync(cancellationToken);

        return questions;
    }
}

