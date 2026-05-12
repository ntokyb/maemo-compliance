using MaemoCompliance.Domain.Common;

namespace MaemoCompliance.Domain.Tenants;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Domain { get; set; }
    public string AdminEmail { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    
    // Licensing & Plans
    public string Edition { get; set; } = "Standard"; // e.g., "Standard", "Enterprise", "GovOnPrem"
    public string Plan { get; set; } = "Pilot"; // e.g., "Pilot", "Standard", "Enterprise"
    public DateTime? LicenseExpiryDate { get; set; }
    public string ModulesEnabledJson { get; set; } = "[]"; // JSON array of module names, e.g., ["Documents", "NCR", "Audits"]
    
    public string? SubscriptionId { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    
    // Branding
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    
    // Microsoft 365 Integration
    public string? AzureAdTenantId { get; set; }
    public string? AzureAdClientId { get; set; }
    public string? AzureAdClientSecret { get; set; } // Will be encrypted later
    public string? SharePointSiteId { get; set; }
    public string? SharePointDriveId { get; set; }

    /// <summary>Full SharePoint site URL (e.g. https://tenant.sharepoint.com/sites/Quality).</summary>
    public string? SharePointSiteUrl { get; set; }

    /// <summary>Document library display name (default Shared Documents).</summary>
    public string? SharePointLibraryName { get; set; }

    /// <summary>Optional app registration client ID used specifically for SharePoint / Graph.</summary>
    public string? SharePointClientId { get; set; }

    /// <summary>Encrypted client secret for SharePointClientId (plaintext only during update API).</summary>
    public string? SharePointClientSecretEncrypted { get; set; }

    /// <summary>Maximum licensed users (enforcement may be added later).</summary>
    public int MaxUsers { get; set; } = 10;

    /// <summary>Maximum storage quota in bytes.</summary>
    public long MaxStorageBytes { get; set; } = 5_368_709_120L;
    
    // Onboarding
    public bool OnboardingCompleted { get; set; } = false;
    public DateTime? OnboardingCompletedAt { get; set; }

    /// <summary>
    /// JSON: { "dismissed": bool } for post-login checklist; step completion is derived from tenant data on read.
    /// </summary>
    public string? OnboardingStepsCompletedJson { get; set; }

    /// <summary>First-time company setup wizard (signup flow); separate from post-login onboarding checklist.</summary>
    public bool SetupComplete { get; set; }

    /// <summary>0 = not started, 1–3 = wizard steps, 4 = finished screen.</summary>
    public int SetupStep { get; set; }

    /// <summary>JSON array of standard codes selected during setup (e.g. ISO 9001).</summary>
    public string? TargetStandardsJson { get; set; }

    /// <summary>Optional profile hints from the setup wizard.</summary>
    public string? Industry { get; set; }

    public string? CompanySize { get; set; }

    public string? City { get; set; }

    public string? Province { get; set; }
}

