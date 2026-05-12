using Maemo.Domain.Common;

namespace Maemo.Domain.Tenants;

public class TenantSettings : TenantOwnedEntity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string? Description { get; set; }
}

