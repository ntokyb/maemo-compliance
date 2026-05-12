using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.AccessRequests;

public class AccessRequest : BaseEntity
{
    public string CompanyName { get; set; } = null!;
    public string Industry { get; set; } = null!;
    public string CompanySize { get; set; } = null!;
    public string ContactName { get; set; } = null!;
    public string ContactEmail { get; set; } = null!;
    public string ContactRole { get; set; } = null!;

    /// <summary>JSON array of ISO / standard labels (e.g. ["ISO 9001","ISO 14001"]).</summary>
    public string TargetStandardsJson { get; set; } = "[]";

    public string ReferralSource { get; set; } = null!;

    public AccessRequestStatus Status { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Populated after approval when tenant is created.</summary>
    public Guid? CreatedTenantId { get; set; }
}
