namespace MaemoCompliance.Application.Tenants.Dtos;

public class TenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Domain { get; set; }
    public string AdminEmail { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? Plan { get; set; }
    public string? SubscriptionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public string[]? ModulesEnabled { get; set; }
    
    // Branding
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    
    // Microsoft 365 Integration
    public string? AzureAdTenantId { get; set; }
    public string? AzureAdClientId { get; set; }
    public string? AzureAdClientSecret { get; set; }
    public string? SharePointSiteId { get; set; }
    public string? SharePointDriveId { get; set; }

    public string? Edition { get; set; }
    public DateTime? LicenseExpiryDate { get; set; }
    public int MaxUsers { get; set; }
    public long MaxStorageBytes { get; set; }

    public string? SharePointSiteUrl { get; set; }
    public string? SharePointLibraryName { get; set; }
    public string? SharePointClientId { get; set; }
    public bool SharePointClientSecretConfigured { get; set; }
}

public class CreateTenantRequest
{
    public string Name { get; set; } = null!;
    public string? Domain { get; set; }
    public string AdminEmail { get; set; } = null!;
    public string? Plan { get; set; }
    public DateTime? TrialEndsAt { get; set; }
}

public class UpdateTenantSettingsRequest
{
    public string Name { get; set; } = null!;
    public string? Domain { get; set; }
    public string AdminEmail { get; set; } = null!;
    public bool IsActive { get; set; }
    public string? Plan { get; set; }
    public DateTime? TrialEndsAt { get; set; }
}

