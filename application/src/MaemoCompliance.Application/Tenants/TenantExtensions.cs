using System.Text.Json;
using MaemoCompliance.Domain.Tenants;

namespace MaemoCompliance.Application.Tenants;

/// <summary>
/// Extension methods for Tenant entity - helper methods for licensing and modules.
/// </summary>
public static class TenantExtensions
{
    /// <summary>
    /// Gets the list of enabled modules from ModulesEnabledJson.
    /// Returns empty list if ModulesEnabledJson is null or invalid.
    /// </summary>
    public static IReadOnlyList<string> GetEnabledModules(this Tenant tenant)
    {
        if (string.IsNullOrWhiteSpace(tenant.ModulesEnabledJson))
        {
            return Array.Empty<string>();
        }

        try
        {
            var modules = JsonSerializer.Deserialize<string[]>(tenant.ModulesEnabledJson);
            return modules ?? Array.Empty<string>();
        }
        catch
        {
            // If JSON is invalid, return empty list
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Sets the enabled modules by serializing to ModulesEnabledJson.
    /// </summary>
    public static void SetEnabledModules(this Tenant tenant, IReadOnlyList<string> modules)
    {
        if (modules == null || modules.Count == 0)
        {
            tenant.ModulesEnabledJson = "[]";
        }
        else
        {
            tenant.ModulesEnabledJson = JsonSerializer.Serialize(modules);
        }
    }

    /// <summary>
    /// Sets the enabled modules by serializing to ModulesEnabledJson (fluent API version).
    /// </summary>
    public static Tenant WithModulesEnabled(this Tenant tenant, IEnumerable<string> modules)
    {
        if (tenant == null)
        {
            throw new ArgumentNullException(nameof(tenant));
        }

        var modulesList = modules?.ToList() ?? new List<string>();
        tenant.ModulesEnabledJson = modulesList.Count == 0 ? "[]" : JsonSerializer.Serialize(modulesList);
        return tenant;
    }
}

