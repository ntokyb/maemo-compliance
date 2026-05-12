namespace MaemoCompliance.Application.Admin.Tenants;

/// <summary>
/// DTO for tenant detail in admin view - comprehensive tenant information.
/// </summary>
public sealed record AdminTenantDetailDto(
    Guid Id,
    string Name,
    string? Domain,
    string AdminEmail,
    bool IsActive,
    string? Edition,
    string? Plan,
    string? SubscriptionId,
    DateTime? TrialEndsAt,
    DateTime? LicenseExpiryDate,
    string[] ModulesEnabled,
    string? LogoUrl,
    string? PrimaryColor,
    DateTime CreatedAt,
    string? CreatedBy,
    DateTime? ModifiedAt,
    string? ModifiedBy,
    int DocumentCount,
    int NcrCount,
    int RiskCount,
    string? SharePointSiteUrl,
    string? SharePointLibraryName,
    string? SharePointClientId,
    bool SharePointClientSecretConfigured,
    string? AzureAdTenantId,
    int MaxUsers,
    long MaxStorageBytes
);

