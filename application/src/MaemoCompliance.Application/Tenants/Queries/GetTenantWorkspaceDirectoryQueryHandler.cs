using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Tenants.Queries;

public class GetTenantWorkspaceDirectoryQueryHandler : IRequestHandler<GetTenantWorkspaceDirectoryQuery, IReadOnlyList<TenantDirectoryRowDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _clock;

    public GetTenantWorkspaceDirectoryQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider clock)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TenantDirectoryRowDto>> Handle(GetTenantWorkspaceDirectoryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var now = _clock.UtcNow;

        var users = await _context.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.Email)
            .Select(u => new TenantDirectoryRowDto
            {
                Email = u.Email,
                Name = u.FullName,
                Role = u.Role.ToString(),
                Status = u.IsActive ? "Active" : "Inactive",
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(cancellationToken);

        var invites = await _context.UserInvitations
            .AsNoTracking()
            .Where(i => i.TenantId == tenantId && i.AcceptedAt == null && i.ExpiresAt > now)
            .OrderBy(i => i.Email)
            .Select(i => new TenantDirectoryRowDto
            {
                Email = i.Email,
                Name = null,
                Role = i.Role.ToString(),
                Status = "Invited",
                LastLoginAt = null
            })
            .ToListAsync(cancellationToken);

        return users.Concat(invites).ToList();
    }
}
