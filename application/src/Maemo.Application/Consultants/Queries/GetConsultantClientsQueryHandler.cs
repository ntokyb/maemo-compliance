using Maemo.Application.Common;
using Maemo.Application.Consultants.Dtos;
using Maemo.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Consultants.Queries;

public class GetConsultantClientsQueryHandler : IRequestHandler<GetConsultantClientsQuery, IReadOnlyList<ConsultantClientDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetConsultantClientsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<ConsultantClientDto>> Handle(GetConsultantClientsQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        // Verify user is a consultant
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == Guid.Parse(currentUserId), cancellationToken);

        if (user == null || user.Role != UserRole.Consultant)
        {
            throw new UnauthorizedAccessException("User is not a consultant.");
        }

        // Get all tenants linked to this consultant
        var clients = await _context.ConsultantTenantLinks
            .Where(l => l.ConsultantUserId == user.Id && l.IsActive)
            .Join(
                _context.Tenants,
                link => link.TenantId,
                tenant => tenant.Id,
                (link, tenant) => new ConsultantClientDto
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    Plan = tenant.Plan,
                    IsActive = link.IsActive && tenant.IsActive
                })
            .ToListAsync(cancellationToken);

        return clients;
    }
}

