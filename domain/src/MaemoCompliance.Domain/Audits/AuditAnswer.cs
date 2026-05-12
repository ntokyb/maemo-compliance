using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Audits;

public class AuditAnswer : TenantOwnedEntity
{
    public Guid AuditRunId { get; set; }
    public Guid AuditQuestionId { get; set; }
    public int Score { get; set; }
    public string? EvidenceFileUrl { get; set; }
    public string? EvidenceFileHash { get; set; } // SHA256 hex string for integrity verification
    public string? Comment { get; set; }
}

