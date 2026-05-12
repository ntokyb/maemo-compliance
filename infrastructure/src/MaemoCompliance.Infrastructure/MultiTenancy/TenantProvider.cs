using MaemoCompliance.Application.Common;

namespace MaemoCompliance.Infrastructure.MultiTenancy;

public class TenantProvider : ITenantProvider
{
    private readonly TenantContext _tenantContext;
    private static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TenantProvider(TenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public Guid GetCurrentTenantId()
    {
        return _tenantContext.TenantId ?? DefaultTenantId;
    }

    public bool HasTenant()
    {
        return _tenantContext.TenantId.HasValue;
    }
}

