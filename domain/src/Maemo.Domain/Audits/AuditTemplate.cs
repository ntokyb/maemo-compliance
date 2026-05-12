using Maemo.Domain.Common;

namespace Maemo.Domain.Audits;

public class AuditTemplate : BaseEntity
{
    public Guid ConsultantUserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}

