using Maemo.Application.Common;
using Maemo.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Consultants.Commands;

public class AssignConsultantToTenantCommandHandler : IRequestHandler<AssignConsultantToTenantCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public AssignConsultantToTenantCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task Handle(AssignConsultantToTenantCommand command, CancellationToken cancellationToken)
    {
        // Verify consultant user exists and has Consultant role
        var consultant = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == command.ConsultantUserId, cancellationToken);

        if (consultant == null)
        {
            throw new KeyNotFoundException($"User with Id {command.ConsultantUserId} not found.");
        }

        if (consultant.Role != UserRole.Consultant)
        {
            throw new InvalidOperationException($"User {command.ConsultantUserId} is not a consultant.");
        }

        // Verify tenant exists
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with Id {command.TenantId} not found.");
        }

        // Check if link already exists
        var existingLink = await _context.ConsultantTenantLinks
            .FirstOrDefaultAsync(
                l => l.ConsultantUserId == command.ConsultantUserId && l.TenantId == command.TenantId,
                cancellationToken);

        if (existingLink != null)
        {
            // If link exists but is inactive, reactivate it
            if (!existingLink.IsActive)
            {
                existingLink.IsActive = true;
                existingLink.ModifiedAt = _dateTimeProvider.UtcNow;
                existingLink.ModifiedBy = _currentUserService.UserId;
            }
            // If already active, do nothing (idempotent)
        }
        else
        {
            // Create new link
            var link = new ConsultantTenantLink
            {
                Id = Guid.NewGuid(),
                ConsultantUserId = command.ConsultantUserId,
                TenantId = command.TenantId,
                IsActive = true,
                CreatedAt = _dateTimeProvider.UtcNow,
                CreatedBy = _currentUserService.UserId
            };

            _context.ConsultantTenantLinks.Add(link);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

