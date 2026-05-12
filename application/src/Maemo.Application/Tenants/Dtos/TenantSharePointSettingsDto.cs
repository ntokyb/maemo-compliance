namespace Maemo.Application.Tenants.Dtos;

/// <summary>
/// SharePoint configuration returned by APIs — never includes a raw client secret.
/// </summary>
public sealed class TenantSharePointSettingsDto
{
    public string? SharePointSiteUrl { get; init; }
    public string? SharePointLibraryName { get; init; }
    public string? SharePointClientId { get; init; }
    public string ClientSecretMasked { get; init; } = "••••••";
}
