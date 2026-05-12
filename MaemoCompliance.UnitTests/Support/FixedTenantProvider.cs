using MaemoCompliance.Application.Common;

namespace MaemoCompliance.UnitTests.Support;

public sealed class FixedTenantProvider : ITenantProvider
{
    public FixedTenantProvider(Guid tenantId) => TenantId = tenantId;

    public Guid TenantId { get; }

    public Guid GetCurrentTenantId() => TenantId;

    public bool HasTenant() => true;
}
