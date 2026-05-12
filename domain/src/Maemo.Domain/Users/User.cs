using Maemo.Domain.Common;

namespace Maemo.Domain.Users;

public class User : BaseEntity
{
    /// <summary>When set, this user belongs to a tenant workspace (not a global consultant-only user).</summary>
    public Guid? TenantId { get; set; }

    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }
}

