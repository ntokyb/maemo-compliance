using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Tenants.Commands;

public class UpdateTenantSettingsCommandHandler : IRequestHandler<UpdateTenantSettingsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTenantSettingsCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateTenantSettingsCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with Id {command.Id} not found.");
        }

        var request = command.Request;

        // Check if name or domain conflicts with another tenant
        var conflictingTenant = await _context.Tenants
            .Where(t => t.Id != command.Id && 
                       (t.Name == request.Name || 
                        (request.Domain != null && t.Domain == request.Domain)))
            .FirstOrDefaultAsync(cancellationToken);

        if (conflictingTenant != null)
        {
            throw new InvalidOperationException(
                $"Tenant with name '{request.Name}' or domain '{request.Domain}' already exists.");
        }

        tenant.Name = request.Name;
        tenant.Domain = request.Domain;
        tenant.AdminEmail = request.AdminEmail;
        tenant.IsActive = request.IsActive;
        tenant.Plan = request.Plan;
        tenant.TrialEndsAt = request.TrialEndsAt;
        tenant.ModifiedAt = _dateTimeProvider.UtcNow;
        tenant.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

