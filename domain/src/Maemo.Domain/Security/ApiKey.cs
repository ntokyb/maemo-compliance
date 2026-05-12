using Maemo.Domain.Common;

namespace Maemo.Domain.Security;

/// <summary>
/// Represents an API key used by tenants to access the Maemo Compliance Engine programmatically.
/// </summary>
public class ApiKey : BaseEntity
{
    /// <summary>
    /// The tenant that owns this API key.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The API key value (random, long token).
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Optional name/description for the API key.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Whether this API key is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

