using Maemo.Application.Common;
using Maemo.Domain.Audits;
using Maemo.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Audits.Commands;

public class CreateAuditTemplateCommandHandler : IRequestHandler<CreateAuditTemplateCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public CreateAuditTemplateCommandHandler(
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

    public async Task<Guid> Handle(CreateAuditTemplateCommand command, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userId = Guid.Parse(currentUserId);

        // Verify user is a consultant
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || user.Role != UserRole.Consultant)
        {
            throw new UnauthorizedAccessException("Only consultants can create audit templates.");
        }

        var template = new AuditTemplate
        {
            Id = Guid.NewGuid(),
            ConsultantUserId = userId,
            Name = command.Name,
            Description = command.Description,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = currentUserId
        };

        _context.AuditTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "AuditTemplate.Created",
            "AuditTemplate",
            template.Id.ToString(),
            new { Name = template.Name, ConsultantUserId = userId },
            cancellationToken);

        return template.Id;
    }
}

