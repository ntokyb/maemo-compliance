using Maemo.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Tenants.Queries;

public class ValidateInviteTokenQueryHandler : IRequestHandler<ValidateInviteTokenQuery, InvitePreviewDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _clock;

    public ValidateInviteTokenQueryHandler(IApplicationDbContext context, IDateTimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<InvitePreviewDto> Handle(ValidateInviteTokenQuery request, CancellationToken cancellationToken)
    {
        var token = request.Token?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            return new InvitePreviewDto(false, null, null, "Token is required.");
        }

        var invite = await _context.UserInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

        if (invite == null)
        {
            return new InvitePreviewDto(false, null, null, "Invitation not found.");
        }

        if (invite.AcceptedAt.HasValue)
        {
            return new InvitePreviewDto(false, null, invite.Email, "This invitation was already used.");
        }

        if (invite.ExpiresAt < _clock.UtcNow)
        {
            return new InvitePreviewDto(false, null, invite.Email, "This invitation has expired.");
        }

        var tenant = await _context.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == invite.TenantId, cancellationToken);

        return new InvitePreviewDto(
            true,
            tenant?.Name,
            invite.Email,
            null);
    }
}
