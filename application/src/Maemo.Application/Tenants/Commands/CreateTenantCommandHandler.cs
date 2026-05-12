using Maemo.Application.Common;
using Maemo.Application.Tenants.Dtos;
using Maemo.Domain.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Tenants.Commands;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public CreateTenantCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var now = _dateTimeProvider.UtcNow;

        // Check if tenant with same name or domain already exists
        var existingTenant = await _context.Tenants
            .Where(t => t.Name == request.Name || 
                       (request.Domain != null && t.Domain == request.Domain))
            .FirstOrDefaultAsync(cancellationToken);

        if (existingTenant != null)
        {
            throw new InvalidOperationException(
                $"Tenant with name '{request.Name}' or domain '{request.Domain}' already exists.");
        }

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Domain = request.Domain,
            AdminEmail = request.AdminEmail,
            IsActive = true,
            Plan = request.Plan ?? "Free",
            CreatedAt = now,
            TrialEndsAt = request.TrialEndsAt,
            CreatedBy = _currentUserService.UserId
        };

        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(cancellationToken);

        return tenant.Id;
    }
}

