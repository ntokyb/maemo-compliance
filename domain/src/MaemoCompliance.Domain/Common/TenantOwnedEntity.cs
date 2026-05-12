namespace MaemoCompliance.Domain.Common;

public abstract class TenantOwnedEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}

