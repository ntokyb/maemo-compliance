using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Tenants;

public class Department : TenantOwnedEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

