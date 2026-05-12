using Maemo.Application.Common;
using Maemo.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Infrastructure.Persistence;

/// <summary>
/// Extension methods for applying tenant filtering to queries.
/// Use these methods in all query handlers to ensure tenant isolation.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Filters a query to only include entities belonging to the current tenant.
    /// </summary>
    /// <typeparam name="T">Entity type that implements TenantOwnedEntity</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="tenantProvider">The tenant provider to get current tenant ID</param>
    /// <returns>Filtered query scoped to current tenant</returns>
    public static IQueryable<T> ForTenant<T>(this IQueryable<T> query, ITenantProvider tenantProvider) 
        where T : TenantOwnedEntity
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        
        // If tenant ID is empty (Guid.Empty), skip filtering to allow seeding/admin operations
        // This should only happen in Development or for admin operations
        if (tenantId == Guid.Empty)
        {
            return query;
        }
        
        return query.Where(e => e.TenantId == tenantId);
    }

    /// <summary>
    /// Filters a query to only include entities belonging to a specific tenant.
    /// </summary>
    /// <typeparam name="T">Entity type that implements TenantOwnedEntity</typeparam>
    /// <param name="query">The query to filter</param>
    /// <param name="tenantId">The tenant ID to filter by</param>
    /// <returns>Filtered query scoped to specified tenant</returns>
    public static IQueryable<T> ForTenant<T>(this IQueryable<T> query, Guid tenantId) 
        where T : TenantOwnedEntity
    {
        if (tenantId == Guid.Empty)
        {
            return query;
        }
        
        return query.Where(e => e.TenantId == tenantId);
    }
}
