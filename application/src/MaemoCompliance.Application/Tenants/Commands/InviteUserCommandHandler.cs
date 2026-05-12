using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Tenants.Commands;

public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;
    private readonly IEmailSender _emailSender;

    public InviteUserCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUser,
        IDateTimeProvider clock,
        IEmailSender emailSender)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _currentUser = currentUser;
        _clock = clock;
        _emailSender = emailSender;
    }

    public async Task<Guid> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        var role = ParseRole(request.RoleName);
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Tenant not found.");

        var now = _clock.UtcNow;
        var activeMembers = await _context.Users.CountAsync(
            u => u.TenantId == tenantId && u.IsActive,
            cancellationToken);
        var pendingInvites = await _context.UserInvitations.CountAsync(
            i => i.TenantId == tenantId && i.AcceptedAt == null && i.ExpiresAt > now,
            cancellationToken);

        if (activeMembers + pendingInvites >= tenant.MaxUsers)
        {
            throw new InvalidOperationException("LICENSE_SEAT_LIMIT");
        }

        var email = request.Email.Trim();
        var emailNorm = email.ToLowerInvariant();

        if (await _context.Users.AnyAsync(
                u => u.TenantId == tenantId && u.Email.ToLower() == emailNorm,
                cancellationToken))
        {
            throw new InvalidOperationException("User already belongs to this workspace.");
        }

        if (await _context.UserInvitations.AnyAsync(
                i => i.TenantId == tenantId && i.Email.ToLower() == emailNorm && i.AcceptedAt == null && i.ExpiresAt > now,
                cancellationToken))
        {
            throw new InvalidOperationException("An invitation for this email is already pending.");
        }

        var token = Guid.NewGuid().ToString("N");
        var invite = new UserInvitation
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            Role = role,
            Token = token,
            ExpiresAt = now.AddHours(48),
            InvitedByUserId = _currentUser.UserId,
            CreatedAt = now,
            CreatedBy = _currentUser.UserId
        };
        _context.UserInvitations.Add(invite);
        await _context.SaveChangesAsync(cancellationToken);

        var baseUrl = (request.PortalBaseUrl ?? "").TrimEnd('/');
        var link = string.IsNullOrEmpty(baseUrl)
            ? $"/accept-invite?token={token}"
            : $"{baseUrl}/accept-invite?token={token}";

        var body =
            $"You've been invited to {tenant.Name}'s Maemo workspace.\n\n" +
            $"Accept your invitation: {link}\n\n" +
            "Use your Microsoft work account when signing in.";

        await _emailSender.SendAsync(
            email,
            $"You've been invited to Maemo — {tenant.Name}",
            body,
            cancellationToken);

        return invite.Id;
    }

    private static UserRole ParseRole(string roleName)
    {
        if (string.Equals(roleName, "TenantAdmin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.TenantAdmin;
        }

        if (string.Equals(roleName, "User", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, "Manager", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, "Viewer", StringComparison.OrdinalIgnoreCase))
        {
            return UserRole.TenantUser;
        }

        throw new ArgumentException("Role must be Admin, TenantAdmin, Manager, Viewer, or User.");
    }
}
