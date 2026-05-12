using Maemo.Application.Common;
using Maemo.Application.Tenants;
using Maemo.Domain.Documents;
using Maemo.Domain.Ncrs;
using Maemo.Domain.Risks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Admin.Tenants;

/// <summary>
/// Handler for admin tenant detail query - returns comprehensive tenant information.
/// </summary>
public class GetAdminTenantDetailQueryHandler : IRequestHandler<GetAdminTenantDetailQuery, AdminTenantDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetAdminTenantDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminTenantDetailDto?> Handle(GetAdminTenantDetailQuery request, CancellationToken cancellationToken)
    {
        // Query tenant by ID (admin view - no tenant filtering)
        // Note: EF Core doesn't support named arguments in expression trees, so we use positional arguments
        // Also, EF Core can't directly deserialize JSON in Select, so we'll fetch the tenant first
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant == null)
        {
            return null;
        }

        // Get modules enabled using helper extension
        var modulesEnabled = tenant.GetEnabledModules().ToArray();

        // Get counts
        var documentCount = await _context.Documents
            .CountAsync(d => d.TenantId == tenant.Id && d.IsCurrentVersion, cancellationToken);
        var ncrCount = await _context.Ncrs
            .CountAsync(n => n.TenantId == tenant.Id, cancellationToken);
        var riskCount = await _context.Risks
            .CountAsync(r => r.TenantId == tenant.Id, cancellationToken);

        var hasSpSecret = !string.IsNullOrEmpty(tenant.SharePointClientSecretEncrypted);

        return new AdminTenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.Domain,
            tenant.AdminEmail,
            tenant.IsActive,
            tenant.Edition,
            tenant.Plan,
            tenant.SubscriptionId,
            tenant.TrialEndsAt,
            tenant.LicenseExpiryDate,
            modulesEnabled,
            tenant.LogoUrl,
            tenant.PrimaryColor,
            tenant.CreatedAt,
            tenant.CreatedBy,
            tenant.ModifiedAt,
            tenant.ModifiedBy,
            documentCount,
            ncrCount,
            riskCount,
            tenant.SharePointSiteUrl,
            tenant.SharePointLibraryName,
            tenant.SharePointClientId,
            hasSpSecret,
            tenant.AzureAdTenantId,
            tenant.MaxUsers,
            tenant.MaxStorageBytes
        );
    }
}

