namespace Maemo.Application.Common;

public interface ITenantProvider
{
    Guid GetCurrentTenantId();
    bool HasTenant();
}

