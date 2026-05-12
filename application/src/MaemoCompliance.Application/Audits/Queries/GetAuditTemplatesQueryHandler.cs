using MaemoCompliance.Application.Audits.Dtos;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditTemplatesQueryHandler : IRequestHandler<GetAuditTemplatesQuery, IReadOnlyList<AuditTemplateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAuditTemplatesQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<AuditTemplateDto>> Handle(GetAuditTemplatesQuery request, CancellationToken cancellationToken)
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
            throw new UnauthorizedAccessException("Only consultants can view audit templates.");
        }

        // Get templates created by this consultant
        var templates = await _context.AuditTemplates
            .Where(t => t.ConsultantUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AuditTemplateDto
            {
                Id = t.Id,
                ConsultantUserId = t.ConsultantUserId,
                Name = t.Name,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return templates;
    }
}

