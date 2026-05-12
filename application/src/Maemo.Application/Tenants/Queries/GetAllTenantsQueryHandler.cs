using Maemo.Application.Common;
using Maemo.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Tenants.Queries;

public class GetAllTenantsQueryHandler : IRequestHandler<GetAllTenantsQuery, IReadOnlyList<TenantDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAllTenantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TenantDto>> Handle(GetAllTenantsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto
            {
                Id = t.Id,
                Name = t.Name,
                Domain = t.Domain,
                AdminEmail = t.AdminEmail,
                IsActive = t.IsActive,
                Plan = t.Plan,
                SubscriptionId = t.SubscriptionId,
                CreatedAt = t.CreatedAt,
                TrialEndsAt = t.TrialEndsAt,
                LogoUrl = t.LogoUrl,
                PrimaryColor = t.PrimaryColor,
                AzureAdTenantId = t.AzureAdTenantId,
                AzureAdClientId = t.AzureAdClientId,
                AzureAdClientSecret = t.AzureAdClientSecret,
                SharePointSiteId = t.SharePointSiteId,
                SharePointDriveId = t.SharePointDriveId
            })
            .ToListAsync(cancellationToken);
    }
}

