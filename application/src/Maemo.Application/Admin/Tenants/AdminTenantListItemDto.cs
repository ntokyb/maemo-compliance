namespace Maemo.Application.Admin.Tenants;

/// <summary>
/// DTO for tenant list item in admin view - basic tenant info with counts.
/// </summary>
public sealed record AdminTenantListItemDto(
    Guid Id,
    string Name,
    string? Domain,
    string AdminEmail,
    bool IsActive,
    string? Edition,
    string? Plan,
    DateTime CreatedAt,
    int DocumentCount,
    int NcrCount,
    int RiskCount,
    string ModulesSummary,
    bool SharePointConnected,
    int MaxUsers
);

