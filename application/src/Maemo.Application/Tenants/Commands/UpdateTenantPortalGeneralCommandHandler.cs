using Maemo.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Tenants.Commands;

public class UpdateTenantPortalGeneralCommandHandler : IRequestHandler<UpdateTenantPortalGeneralCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTenantPortalGeneralCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateTenantPortalGeneralCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with Id {tenantId} not found.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            throw new ArgumentException("Name is required.");
        }

        var conflictingTenant = await _context.Tenants
            .AnyAsync(t => t.Id != tenantId && t.Name == command.Name.Trim(), cancellationToken);

        if (conflictingTenant)
        {
            throw new InvalidOperationException($"Another tenant already uses the name '{command.Name.Trim()}'.");
        }

        tenant.Name = command.Name.Trim();
        tenant.LogoUrl = string.IsNullOrWhiteSpace(command.LogoUrl) ? null : command.LogoUrl.Trim();
        tenant.PrimaryColor = string.IsNullOrWhiteSpace(command.PrimaryColor) ? null : command.PrimaryColor.Trim();
        tenant.ModifiedAt = _dateTimeProvider.UtcNow;
        tenant.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
