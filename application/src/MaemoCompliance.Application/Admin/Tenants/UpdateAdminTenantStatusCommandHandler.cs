using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Admin.Tenants;

/// <summary>
/// Handler for updating tenant status - allows activating/suspending tenants.
/// </summary>
public class UpdateAdminTenantStatusCommandHandler : IRequestHandler<UpdateAdminTenantStatusCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UpdateAdminTenantStatusCommandHandler(
        IApplicationDbContext context,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(UpdateAdminTenantStatusCommand request, CancellationToken cancellationToken)
    {
        // Query tenant by ID (admin view - no tenant filtering)
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID {request.TenantId} not found.");
        }

        var oldStatus = tenant.IsActive ? "Active" : "Suspended";

        // Update tenant status based on Status string
        // Status values: "Active", "Suspended"
        // TODO: Consider using TenantStatus enum instead of string for type safety
        switch (request.Status.ToLowerInvariant())
        {
            case "active":
                tenant.IsActive = true;
                break;
            case "suspended":
                tenant.IsActive = false;
                break;
            default:
                throw new ArgumentException($"Invalid status value: {request.Status}. Valid values are: Active, Suspended.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        var newStatus = tenant.IsActive ? "Active" : "Suspended";
        if (oldStatus != newStatus)
        {
            await _businessAuditLogger.LogAsync(
                "Tenant.StatusChanged",
                "Tenant",
                request.TenantId.ToString(),
                new { OldStatus = oldStatus, NewStatus = newStatus },
                cancellationToken);
        }
    }
}

