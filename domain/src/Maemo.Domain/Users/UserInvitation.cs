using Maemo.Domain.Common;

namespace Maemo.Domain.Users;

public class UserInvitation : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = null!;
    public UserRole Role { get; set; }
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public string? InvitedByUserId { get; set; }
}
