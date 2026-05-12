using Maemo.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Consultants.Commands;

public class RemoveConsultantFromTenantCommandHandler : IRequestHandler<RemoveConsultantFromTenantCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public RemoveConsultantFromTenantCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task Handle(RemoveConsultantFromTenantCommand command, CancellationToken cancellationToken)
    {
        var link = await _context.ConsultantTenantLinks
            .FirstOrDefaultAsync(
                l => l.ConsultantUserId == command.ConsultantUserId && l.TenantId == command.TenantId,
                cancellationToken);

        if (link == null)
        {
            throw new KeyNotFoundException(
                $"Consultant-Tenant link not found for ConsultantId {command.ConsultantUserId} and TenantId {command.TenantId}.");
        }

        // Soft delete by setting IsActive to false
        link.IsActive = false;
        link.ModifiedAt = _dateTimeProvider.UtcNow;
        link.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

