using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Audits;
using MaemoCompliance.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Commands;

public class AddAuditQuestionCommandHandler : IRequestHandler<AddAuditQuestionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public AddAuditQuestionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task<Guid> Handle(AddAuditQuestionCommand command, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userId = Guid.Parse(currentUserId);

        // Verify template exists and belongs to current consultant
        var template = await _context.AuditTemplates
            .FirstOrDefaultAsync(t => t.Id == command.AuditTemplateId, cancellationToken);

        if (template == null)
        {
            throw new KeyNotFoundException($"Audit template with Id {command.AuditTemplateId} not found.");
        }

        // Verify user is a consultant and owns the template
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || user.Role != UserRole.Consultant)
        {
            throw new UnauthorizedAccessException("Only consultants can add audit questions.");
        }

        if (template.ConsultantUserId != userId)
        {
            throw new UnauthorizedAccessException("You can only add questions to your own audit templates.");
        }

        var question = new AuditQuestion
        {
            Id = Guid.NewGuid(),
            AuditTemplateId = command.AuditTemplateId,
            Category = command.Category,
            QuestionText = command.QuestionText,
            MaxScore = command.MaxScore,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = currentUserId
        };

        _context.AuditQuestions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "AuditTemplate.QuestionAdded",
            "AuditTemplate",
            command.AuditTemplateId.ToString(),
            new { QuestionId = question.Id, Category = question.Category, MaxScore = question.MaxScore },
            cancellationToken);

        return question.Id;
    }
}

