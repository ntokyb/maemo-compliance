using Maemo.Application.Common;
using Maemo.Application.Tenants.Queries;
using Maemo.Domain.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Identity;
using System.Net;

namespace Maemo.Infrastructure.Storage;

public class SharePointFileStorageProvider : IFileStorageProvider
{
    private readonly IGraphService _graphService;
    private readonly IMediator _mediator;
    private readonly IDeploymentContext _deploymentContext;
    private readonly ILogger<SharePointFileStorageProvider> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITenantProvider _tenantProvider;

    public SharePointFileStorageProvider(
        IGraphService graphService,
        IMediator mediator,
        IDeploymentContext deploymentContext,
        ILogger<SharePointFileStorageProvider> logger,
        IHttpClientFactory httpClientFactory,
        ITenantProvider tenantProvider)
    {
        _graphService = graphService;
        _mediator = mediator;
        _deploymentContext = deploymentContext;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _tenantProvider = tenantProvider;

        if (_deploymentContext.IsGovOnPrem)
        {
            _logger.LogWarning(
                "SharePointFileStorageProvider should not be used in GovOnPrem mode. " +
                "This indicates a configuration error.");
        }
    }

    public async Task<string> SaveAsync(
        Guid tenantId,
        Stream content,
        string fileName,
        string category,
        CancellationToken cancellationToken = default)
    {
        if (_deploymentContext.IsGovOnPrem)
        {
            throw new InvalidOperationException(
                "SharePointFileStorageProvider cannot be used in GovOnPrem deployment mode. " +
                "Use LocalFileStorageProvider instead.");
        }

        // Get tenant to retrieve Microsoft 365 credentials
        var tenantQuery = new GetTenantByIdQuery { Id = tenantId };
        var tenant = await _mediator.Send(tenantQuery, cancellationToken);

        if (tenant == null ||
            string.IsNullOrWhiteSpace(tenant.AzureAdTenantId) ||
            string.IsNullOrWhiteSpace(tenant.AzureAdClientId) ||
            string.IsNullOrWhiteSpace(tenant.AzureAdClientSecret))
        {
            throw new InvalidOperationException(
                $"Tenant {tenantId} does not have Microsoft 365 integration configured. " +
                "SharePoint storage requires M365 credentials.");
        }

        // Upload to SharePoint via Microsoft Graph
        var folderPath = $"{category ?? "General"}";
        
        await _graphService.UploadDocumentAsync(
            tenantId,
            content,
            fileName,
            folderPath,
            cancellationToken);

        // Construct SharePoint path
        // Format: sites/{siteId}/drives/{driveId}/root:/{folderPath}/{fileName}
        var siteId = tenant.SharePointSiteId ?? "default";
        var driveId = tenant.SharePointDriveId ?? "default";
        var storagePath = $"sites/{siteId}/drives/{driveId}/root:/{folderPath}/{fileName}";

        _logger.LogInformation(
            "File saved to SharePoint: {StoragePath}",
            storagePath);

        return storagePath;
    }

    public async Task<Stream?> GetAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (_deploymentContext.IsGovOnPrem)
        {
            throw new InvalidOperationException(
                "SharePointFileStorageProvider cannot be used in GovOnPrem deployment mode.");
        }

        _logger.LogInformation(
            "SharePoint GetAsync called - TenantId: {TenantId}, StoragePath: {StoragePath}",
            tenantId,
            storagePath);

        try
        {
            // Get tenant credentials
            var credentials = await GetTenantCredentialsAsync(tenantId, cancellationToken);
            if (credentials == null)
            {
                _logger.LogWarning(
                    "No Microsoft 365 credentials found for tenant {TenantId}. Cannot retrieve file.",
                    tenantId);
                return null;
            }

            // Get access token
            var accessToken = await GetAccessTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError(
                    "Failed to acquire access token for tenant {TenantId}",
                    tenantId);
                throw new InvalidOperationException("Failed to acquire SharePoint access token.");
            }

            // Build SharePoint site URL
            var siteUrl = await GetSharePointSiteUrlAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(siteUrl))
            {
                _logger.LogError(
                    "SharePoint site URL not configured for tenant {TenantId}",
                    tenantId);
                throw new InvalidOperationException("SharePoint site URL not configured.");
            }

            // Convert storage path to SharePoint server-relative URL
            var serverRelativeUrl = ConvertStoragePathToServerRelativeUrl(storagePath, credentials);

            // Build SharePoint REST API URL
            var apiUrl = $"{siteUrl.TrimEnd('/')}/_api/web/GetFileByServerRelativeUrl('{Uri.EscapeDataString(serverRelativeUrl)}')/$value";

            // Create HTTP client and request
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _logger.LogInformation(
                "Downloading file from SharePoint - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}",
                tenantId,
                serverRelativeUrl);

            var response = await httpClient.GetAsync(apiUrl, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "File not found in SharePoint - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}",
                    tenantId,
                    serverRelativeUrl);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError(
                    "Access denied to SharePoint file - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}, StatusCode: {StatusCode}",
                    tenantId,
                    serverRelativeUrl,
                    response.StatusCode);
                throw new UnauthorizedAccessException(
                    $"Access denied to SharePoint file. Status: {response.StatusCode}");
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation(
                "Successfully downloaded file from SharePoint - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}",
                tenantId,
                serverRelativeUrl);

            return await response.Content.ReadAsStreamAsync(cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error retrieving file from SharePoint - TenantId: {TenantId}, StoragePath: {StoragePath}",
                tenantId,
                storagePath);
            throw new InvalidOperationException(
                $"Failed to retrieve file from SharePoint: {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving file from SharePoint - TenantId: {TenantId}, StoragePath: {StoragePath}",
                tenantId,
                storagePath);
            throw;
        }
    }

    public async Task DeleteAsync(
        Guid tenantId,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        if (_deploymentContext.IsGovOnPrem)
        {
            throw new InvalidOperationException(
                "SharePointFileStorageProvider cannot be used in GovOnPrem deployment mode.");
        }

        _logger.LogInformation(
            "SharePoint DeleteAsync called - TenantId: {TenantId}, StoragePath: {StoragePath}",
            tenantId,
            storagePath);

        try
        {
            // Get tenant credentials
            var credentials = await GetTenantCredentialsAsync(tenantId, cancellationToken);
            if (credentials == null)
            {
                _logger.LogWarning(
                    "No Microsoft 365 credentials found for tenant {TenantId}. Cannot delete file.",
                    tenantId);
                throw new InvalidOperationException(
                    $"Tenant {tenantId} does not have Microsoft 365 integration configured.");
            }

            // Get access token
            var accessToken = await GetAccessTokenAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError(
                    "Failed to acquire access token for tenant {TenantId}",
                    tenantId);
                throw new InvalidOperationException("Failed to acquire SharePoint access token.");
            }

            // Build SharePoint site URL
            var siteUrl = await GetSharePointSiteUrlAsync(credentials, cancellationToken);
            if (string.IsNullOrEmpty(siteUrl))
            {
                _logger.LogError(
                    "SharePoint site URL not configured for tenant {TenantId}",
                    tenantId);
                throw new InvalidOperationException("SharePoint site URL not configured.");
            }

            // Convert storage path to SharePoint server-relative URL
            var serverRelativeUrl = ConvertStoragePathToServerRelativeUrl(storagePath, credentials);

            // Build SharePoint REST API URL
            var apiUrl = $"{siteUrl.TrimEnd('/')}/_api/web/GetFileByServerRelativeUrl('{Uri.EscapeDataString(serverRelativeUrl)}')";

            // Create HTTP client and request
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("IF-MATCH", "*");
            httpClient.DefaultRequestHeaders.Add("X-HTTP-Method", "DELETE");

            _logger.LogInformation(
                "Deleting file from SharePoint - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}",
                tenantId,
                serverRelativeUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Headers.Add("IF-MATCH", "*");
            request.Headers.Add("X-HTTP-Method", "DELETE");

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "File not found in SharePoint (may already be deleted) - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}",
                    tenantId,
                    serverRelativeUrl);
                // Treat 404 as success (file already deleted)
                return;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError(
                    "Access denied to delete SharePoint file - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}, StatusCode: {StatusCode}",
                    tenantId,
                    serverRelativeUrl,
                    response.StatusCode);
                throw new UnauthorizedAccessException(
                    $"Access denied to delete SharePoint file. Status: {response.StatusCode}");
            }

            if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation(
                    "Successfully deleted file from SharePoint - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}",
                    tenantId,
                    serverRelativeUrl);
                return;
            }

            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error deleting file from SharePoint - TenantId: {TenantId}, StoragePath: {StoragePath}",
                tenantId,
                storagePath);
            throw new InvalidOperationException(
                $"Failed to delete file from SharePoint: {ex.Message}",
                ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting file from SharePoint - TenantId: {TenantId}, StoragePath: {StoragePath}",
                tenantId,
                storagePath);
            throw;
        }
    }

    private async Task<Microsoft365Credentials?> GetTenantCredentialsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantQuery = new GetTenantByIdQuery { Id = tenantId };
        var tenant = await _mediator.Send(tenantQuery, cancellationToken);

        if (tenant == null ||
            string.IsNullOrWhiteSpace(tenant.AzureAdTenantId) ||
            string.IsNullOrWhiteSpace(tenant.AzureAdClientId) ||
            string.IsNullOrWhiteSpace(tenant.AzureAdClientSecret))
        {
            return null;
        }

        return new Microsoft365Credentials
        {
            TenantId = tenant.AzureAdTenantId,
            ClientId = tenant.AzureAdClientId,
            ClientSecret = tenant.AzureAdClientSecret,
            SharePointSiteId = tenant.SharePointSiteId,
            SharePointDriveId = tenant.SharePointDriveId
        };
    }

    private async Task<string> GetAccessTokenAsync(Microsoft365Credentials credentials, CancellationToken cancellationToken)
    {
        try
        {
            var credential = new ClientSecretCredential(
                credentials.TenantId,
                credentials.ClientId,
                credentials.ClientSecret);

            // Get SharePoint site URL to extract domain
            var siteUrl = await GetSharePointSiteUrlAsync(credentials, cancellationToken);
            var siteUri = new Uri(siteUrl);
            var tenantDomain = siteUri.Host; // e.g., "contoso.sharepoint.com"

            // Request token for SharePoint - use .default scope for the SharePoint site
            var scope = $"https://{tenantDomain}/.default";

            var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { scope });

            var token = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);
            return token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to acquire access token - TenantId: {TenantId}, ClientId: {ClientId}",
                credentials.TenantId,
                credentials.ClientId);
            throw;
        }
    }

    private Task<string> GetSharePointSiteUrlAsync(Microsoft365Credentials credentials, CancellationToken cancellationToken)
    {
        // If SharePointSiteId is a full URL, use it directly
        if (!string.IsNullOrWhiteSpace(credentials.SharePointSiteId))
        {
            if (Uri.TryCreate(credentials.SharePointSiteId, UriKind.Absolute, out var uri))
            {
                return Task.FromResult(credentials.SharePointSiteId);
            }
            
            // If it's just a site ID/name, construct URL
            // Note: This requires knowing the tenant SharePoint domain
            // For MVP, we'll try to extract from Azure AD tenant ID or require full URL
            // In production, query Microsoft Graph /organization to get verifiedDomains
            // Format: https://{tenant}.sharepoint.com/sites/{siteId}
            
            // Try to get tenant domain from configuration or use a default pattern
            // This is a simplification - in production, query Microsoft Graph
            var tenantDomain = ExtractTenantDomainFromCredentials(credentials);
            return Task.FromResult($"https://{tenantDomain}/sites/{credentials.SharePointSiteId}");
        }

        // Fallback: Use root site
        var tenantDomainFallback = ExtractTenantDomainFromCredentials(credentials);
        return Task.FromResult($"https://{tenantDomainFallback}");
    }

    private string ExtractTenantDomainFromCredentials(Microsoft365Credentials credentials)
    {
        // In production, query Microsoft Graph API: GET /organization?$select=verifiedDomains
        // to get the actual SharePoint domain (e.g., contoso.sharepoint.com)
        
        // For MVP: If SharePointSiteId was a full URL, we already handled it above
        // If not, we need tenant domain configuration or Graph API query
        
        // Simplified approach: Assume tenant domain can be derived
        // In production, this should query Microsoft Graph or be configured
        throw new InvalidOperationException(
            "SharePoint tenant domain cannot be determined. " +
            "Please configure SharePointSiteId as a full URL (e.g., https://contoso.sharepoint.com/sites/sitename) " +
            "or configure tenant SharePoint domain.");
    }

    private string ConvertStoragePathToServerRelativeUrl(string storagePath, Microsoft365Credentials credentials)
    {
        // Storage path format: sites/{siteId}/drives/{driveId}/root:/{folderPath}/{fileName}
        // SharePoint server-relative URL format: /sites/{siteId}/Shared Documents/{folderPath}/{fileName}
        // or: /sites/{siteId}/{libraryName}/{folderPath}/{fileName}

        if (storagePath.StartsWith("sites/", StringComparison.OrdinalIgnoreCase))
        {
            // Extract path after "root:"
            var rootIndex = storagePath.IndexOf("root:", StringComparison.OrdinalIgnoreCase);
            if (rootIndex >= 0)
            {
                var relativePath = storagePath.Substring(rootIndex + 5); // Skip "root:"
                var sitePart = storagePath.Substring(0, rootIndex).TrimEnd('/');
                
                // Replace "drives/{driveId}" with library name (default: "Shared Documents")
                var libraryName = "Shared Documents"; // Default SharePoint document library
                
                // Build server-relative URL
                return $"/{sitePart}/{libraryName}{relativePath}";
            }
        }

        // If format doesn't match expected pattern, assume it's already a server-relative URL
        if (!storagePath.StartsWith("/"))
        {
            return "/" + storagePath;
        }

        return storagePath;
    }

    private class Microsoft365Credentials
    {
        public string TenantId { get; set; } = null!;
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string? SharePointSiteId { get; set; }
        public string? SharePointDriveId { get; set; }
    }
}

