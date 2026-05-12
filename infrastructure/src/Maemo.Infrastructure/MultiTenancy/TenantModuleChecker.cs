using Maemo.Application.Tenants;
using Maemo.Domain.Tenants;
using Microsoft.AspNetCore.Http;

namespace Maemo.Infrastructure.MultiTenancy;

/// <summary>
/// Implementation of ITenantModuleChecker that reads from HttpContext.Items.
/// </summary>
public class TenantModuleChecker : ITenantModuleChecker
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantModuleChecker(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool HasModule(string moduleName)
    {
        var modules = GetEnabledModules();
        return modules.Contains(moduleName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a module is enabled (alias for HasModule for consistency).
    /// </summary>
    public bool IsEnabled(string moduleName) => HasModule(moduleName);

    /// <summary>
    /// Checks if a module is enabled (alternative name for consistency with requirements).
    /// </summary>
    public bool IsModuleEnabled(string moduleName) => HasModule(moduleName);

    public IReadOnlyList<string> GetEnabledModules()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Items.TryGetValue("TenantModules", out var modulesObj) == true &&
            modulesObj is IReadOnlyList<string> modules)
        {
            return modules;
        }

        // If no modules found in context, return empty list (fail closed)
        return Array.Empty<string>();
    }
}

