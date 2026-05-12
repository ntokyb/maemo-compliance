namespace Maemo.Application.Admin.Tenants;

/// <summary>
/// DTO for tenant branding information.
/// </summary>
public sealed record AdminTenantBrandingDto(
    string? LogoUrl,
    string? PrimaryColor
);

