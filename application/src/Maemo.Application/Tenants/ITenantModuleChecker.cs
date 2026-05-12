namespace Maemo.Application.Tenants;

/// <summary>
/// Service for checking if a tenant has access to specific modules.
/// </summary>
public interface ITenantModuleChecker
{
    /// <summary>
    /// Checks if the current tenant has access to the specified module.
    /// </summary>
    bool HasModule(string moduleName);

    /// <summary>
    /// Checks if a module is enabled (alias for HasModule for consistency).
    /// </summary>
    bool IsEnabled(string moduleName);

    /// <summary>
    /// Checks if a module is enabled (alternative name for consistency with requirements).
    /// </summary>
    bool IsModuleEnabled(string moduleName);

    /// <summary>
    /// Gets all enabled modules for the current tenant.
    /// </summary>
    IReadOnlyList<string> GetEnabledModules();
}

