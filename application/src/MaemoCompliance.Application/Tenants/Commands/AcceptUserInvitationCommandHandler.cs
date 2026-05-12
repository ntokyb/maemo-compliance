using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Tenants.Commands;

public class AcceptUserInvitationCommandHandler : IRequestHandler<AcceptUserInvitationCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public AcceptUserInvitationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task Handle(AcceptUserInvitationCommand request, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            throw new ArgumentException("Token is required.");
        }

        var email = _currentUser.UserEmail?.Trim();
        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("Your account email could not be determined. Sign in again.");
        }

        var invite = await _context.UserInvitations
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

        if (invite == null || invite.AcceptedAt.HasValue || invite.ExpiresAt < _clock.UtcNow)
        {
            throw new InvalidOperationException("INVITE_INVALID");
        }

        if (!string.Equals(invite.Email.Trim(), email, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("INVITE_EMAIL_MISMATCH");
        }

        var existing = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLowerInvariant(), cancellationToken);

        if (existing != null)
        {
            if (existing.TenantId.HasValue && existing.TenantId.Value != invite.TenantId)
            {
                throw new InvalidOperationException("This email is already assigned to another workspace.");
            }

            existing.TenantId = invite.TenantId;
            existing.Role = invite.Role;
            existing.ModifiedAt = _clock.UtcNow;
            existing.ModifiedBy = _currentUser.UserId;
        }
        else
        {
            var name = email;
            _context.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                TenantId = invite.TenantId,
                Email = email,
                FullName = name,
                Role = invite.Role,
                IsActive = true,
                CreatedAt = _clock.UtcNow,
                CreatedBy = _currentUser.UserId
            });
        }

        invite.AcceptedAt = _clock.UtcNow;
        invite.ModifiedAt = _clock.UtcNow;
        invite.ModifiedBy = _currentUser.UserId;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
