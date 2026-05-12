using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Admin.Tenants;

/// <summary>
/// Handler for updating tenant branding.
/// </summary>
public class UpdateTenantBrandingCommandHandler : IRequestHandler<UpdateTenantBrandingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UpdateTenantBrandingCommandHandler(
        IApplicationDbContext context,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(UpdateTenantBrandingCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID {request.TenantId} was not found.");
        }

        var oldLogoUrl = tenant.LogoUrl;
        var oldPrimaryColor = tenant.PrimaryColor;

        tenant.LogoUrl = request.LogoUrl;
        tenant.PrimaryColor = request.PrimaryColor;
        tenant.ModifiedAt = DateTime.UtcNow;
        tenant.ModifiedBy = "System"; // TODO: Get from current user context

        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Tenant.BrandingUpdated",
            "Tenant",
            request.TenantId.ToString(),
            new { OldLogoUrl = oldLogoUrl, NewLogoUrl = request.LogoUrl, OldPrimaryColor = oldPrimaryColor, NewPrimaryColor = request.PrimaryColor },
            cancellationToken);
    }
}

