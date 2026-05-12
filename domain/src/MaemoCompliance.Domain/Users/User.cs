using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Users;

public class User : BaseEntity
{
    /// <summary>When set, this user belongs to a tenant workspace (not a global consultant-only user).</summary>
    public Guid? TenantId { get; set; }

    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>First-run wizard after accepting an invite; independent of tenant onboarding checklist.</summary>
    public bool OnboardingComplete { get; set; }

    public string? JobTitle { get; set; }
    public string? Phone { get; set; }

    /// <summary>Optional address captured during user onboarding.</summary>
    public string? AddressLine { get; set; }

    /// <summary>JSON array from onboarding wizard (e.g. ISO standards).</summary>
    public string? ComplianceStandardsJson { get; set; }
}

