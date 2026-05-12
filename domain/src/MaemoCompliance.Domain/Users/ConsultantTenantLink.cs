using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Users;

public class ConsultantTenantLink : BaseEntity
{
    public Guid ConsultantUserId { get; set; }
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; } = true;
}

