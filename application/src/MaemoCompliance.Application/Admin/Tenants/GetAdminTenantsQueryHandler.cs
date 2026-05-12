using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Admin.Tenants;

/// <summary>
/// Handler for admin tenants list query - returns all tenants with basic counts.
/// </summary>
public class GetAdminTenantsQueryHandler : IRequestHandler<GetAdminTenantsQuery, IReadOnlyList<AdminTenantListItemDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAdminTenantsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminTenantListItemDto>> Handle(GetAdminTenantsQuery request, CancellationToken cancellationToken)
    {
        var tenants = await _context.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .ToListAsync(cancellationToken);

        if (tenants.Count == 0)
        {
            return Array.Empty<AdminTenantListItemDto>();
        }

        var ids = tenants.Select(t => t.Id).ToList();

        var docCounts = await _context.Documents
            .AsNoTracking()
            .Where(d => d.IsCurrentVersion && ids.Contains(d.TenantId))
            .GroupBy(d => d.TenantId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var ncrCounts = await _context.Ncrs
            .AsNoTracking()
            .Where(n => ids.Contains(n.TenantId))
            .GroupBy(n => n.TenantId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var riskCounts = await _context.Risks
            .AsNoTracking()
            .Where(r => ids.Contains(r.TenantId))
            .GroupBy(r => r.TenantId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);

        var result = new List<AdminTenantListItemDto>(tenants.Count);
        foreach (var t in tenants)
        {
            var modules = t.GetEnabledModules();
            string modulesSummary;
            if (modules.Count == 0)
            {
                modulesSummary = "—";
            }
            else
            {
                var take = modules.Take(4).ToArray();
                modulesSummary = string.Join(", ", take);
                if (modules.Count > 4)
                {
                    modulesSummary += "…";
                }
            }

            var credsReady = !string.IsNullOrWhiteSpace(t.SharePointClientId ?? t.AzureAdClientId) &&
                             (!string.IsNullOrEmpty(t.SharePointClientSecretEncrypted) ||
                              !string.IsNullOrEmpty(t.AzureAdClientSecret));
            var sharePointConnected = !string.IsNullOrWhiteSpace(t.SharePointSiteUrl) && credsReady;

            result.Add(new AdminTenantListItemDto(
                t.Id,
                t.Name,
                t.Domain,
                t.AdminEmail,
                t.IsActive,
                t.Edition,
                t.Plan,
                t.CreatedAt,
                docCounts.GetValueOrDefault(t.Id),
                ncrCounts.GetValueOrDefault(t.Id),
                riskCounts.GetValueOrDefault(t.Id),
                modulesSummary,
                sharePointConnected,
                t.MaxUsers));
        }

        return result;
    }
}

