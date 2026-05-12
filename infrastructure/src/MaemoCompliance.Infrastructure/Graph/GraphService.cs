using System.Net;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MaemoCompliance.Infrastructure.Graph;

public class GraphService : IGraphService
{
    private readonly ILogger<GraphService> _logger;
    private readonly IMediator _mediator;

    public GraphService(
        ILogger<GraphService> logger,
        IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    private async Task<Microsoft365Credentials?> GetTenantCredentialsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        // Get tenant to retrieve Microsoft 365 credentials
        var query = new GetTenantByIdQuery { Id = tenantId };
        var tenant = await _mediator.Send(query, cancellationToken);

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

    private object? BuildGraphClient(Microsoft365Credentials credentials)
    {
        // Phase 3: Prepare signature for building GraphClient per tenant
        // This will be implemented in a future phase to actually create
        // a Microsoft.Graph.GraphServiceClient using the tenant-specific credentials
        // 
        // Example implementation:
        // var clientCredential = new ClientSecretCredential(
        //     credentials.TenantId,
        //     credentials.ClientId,
        //     credentials.ClientSecret);
        // return new GraphServiceClient(clientCredential);
        
        // For now, return null to indicate credentials are available but not yet implemented
        return null;
    }

    internal class Microsoft365Credentials
    {
        public string TenantId { get; set; } = null!;
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string? SharePointSiteId { get; set; }
        public string? SharePointDriveId { get; set; }
    }

    public async Task UploadDocumentAsync(
        Guid tenantId,
        Stream content,
        string fileName,
        string folderPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "GraphService.UploadDocumentAsync called - TenantId: {TenantId}, FileName: {FileName}, FolderPath: {FolderPath}",
            tenantId,
            fileName,
            folderPath);

        var credentials = await GetTenantCredentialsAsync(tenantId, cancellationToken);

        if (credentials == null)
        {
            _logger.LogWarning(
                "No Microsoft 365 credentials found for tenant {TenantId}. Cannot upload to SharePoint.",
                tenantId);
            throw new InvalidOperationException(
                $"Tenant {tenantId} does not have Microsoft 365 integration configured. SharePoint upload requires M365 credentials.");
        }

        var siteUrl = await GetSharePointSiteUrlAsync(credentials, cancellationToken);
        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            _logger.LogError(
                "SharePoint site URL not configured for tenant {TenantId}",
                tenantId);
            throw new InvalidOperationException("SharePoint site URL not configured.");
        }

        string accessToken;
        try
        {
            accessToken = await GetSharePointAccessTokenAsync(credentials, siteUrl, cancellationToken);
        }
        catch (Exception ex) when (ex is CredentialUnavailableException or AuthenticationFailedException)
        {
            _logger.LogError(
                ex,
                "Failed to acquire SharePoint access token for tenant {TenantId} (check client secret / app registration).",
                tenantId);
            throw new InvalidOperationException(
                "SharePoint authentication failed: invalid, expired, or unavailable Microsoft 365 credentials.",
                ex);
        }

        if (string.IsNullOrEmpty(accessToken))
        {
            _logger.LogError(
                "Failed to acquire access token for tenant {TenantId}",
                tenantId);
            throw new InvalidOperationException("Failed to acquire SharePoint access token.");
        }

        var safeLeaf = string.IsNullOrWhiteSpace(fileName)
            ? "file"
            : Path.GetFileName(fileName.Trim());

        var siteId = credentials.SharePointSiteId ?? "default";
        var driveId = credentials.SharePointDriveId ?? "default";
        var storagePathForConversion = $"sites/{siteId}/drives/{driveId}/root:/{folderPath}/{safeLeaf}";
        var fileServerRelativePath = ConvertStoragePathToServerRelativeUrl(storagePathForConversion, credentials);

        var lastSlash = fileServerRelativePath.LastIndexOf('/');
        if (lastSlash < 0 || lastSlash >= fileServerRelativePath.Length - 1)
        {
            throw new InvalidOperationException(
                $"Invalid SharePoint server-relative path for upload: {fileServerRelativePath}");
        }

        var folderServerRelativePath = fileServerRelativePath[..lastSlash];
        var oDataFileName = safeLeaf.Replace("'", "''", StringComparison.Ordinal);

        var encodedFolder = Uri.EscapeDataString(folderServerRelativePath);
        var apiUrl =
            $"{siteUrl.TrimEnd('/')}/_api/web/GetFolderByServerRelativeUrl('{encodedFolder}')/Files/Add(url='{oDataFileName}',overwrite=true)";

        _logger.LogInformation(
            "Uploading file to SharePoint - TenantId: {TenantId}, FolderServerRelativeUrl: {FolderServerRelativeUrl}, FileName: {FileName}",
            tenantId,
            folderServerRelativePath,
            safeLeaf);

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.PostAsync(apiUrl, streamContent, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogError(
                "Access denied uploading SharePoint file - TenantId: {TenantId}, FolderServerRelativeUrl: {FolderServerRelativeUrl}, StatusCode: {StatusCode}",
                tenantId,
                folderServerRelativePath,
                response.StatusCode);
            throw new UnauthorizedAccessException(
                $"Access denied uploading to SharePoint. Status: {response.StatusCode}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "SharePoint upload failed - TenantId: {TenantId}, StatusCode: {StatusCode}, Response: {Body}",
                tenantId,
                response.StatusCode,
                body);
            throw new InvalidOperationException(
                $"SharePoint file upload failed with status {(int)response.StatusCode}: {response.ReasonPhrase}. {body}");
        }

        _logger.LogInformation(
            "Successfully uploaded file to SharePoint - TenantId: {TenantId}, ServerRelativeUrl: {ServerRelativeUrl}",
            tenantId,
            fileServerRelativePath);
    }

    private static async Task<string> GetSharePointAccessTokenAsync(
        Microsoft365Credentials credentials,
        string siteUrl,
        CancellationToken cancellationToken)
    {
        var credential = new ClientSecretCredential(
            credentials.TenantId,
            credentials.ClientId,
            credentials.ClientSecret);

        var siteUri = new Uri(siteUrl);
        var tenantDomain = siteUri.Host;
        var scope = $"https://{tenantDomain}/.default";
        var tokenRequestContext = new TokenRequestContext([scope]);
        var token = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);
        return token.Token;
    }

    private Task<string> GetSharePointSiteUrlAsync(Microsoft365Credentials credentials, CancellationToken _)
    {
        if (!string.IsNullOrWhiteSpace(credentials.SharePointSiteId))
        {
            if (credentials.SharePointSiteId.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                credentials.SharePointSiteId.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(credentials.SharePointSiteId);
            }

            var tenantDomain = ExtractTenantDomainFromCredentials(credentials);
            return Task.FromResult($"https://{tenantDomain}/sites/{credentials.SharePointSiteId}");
        }

        var tenantDomainFallback = ExtractTenantDomainFromCredentials(credentials);
        return Task.FromResult($"https://{tenantDomainFallback}");
    }

    private static string ExtractTenantDomainFromCredentials(Microsoft365Credentials _)
    {
        throw new InvalidOperationException(
            "SharePoint tenant domain cannot be determined. " +
            "Please configure SharePointSiteId as a full URL (e.g., https://contoso.sharepoint.com/sites/sitename) " +
            "or configure tenant SharePoint domain.");
    }

    private static string ConvertStoragePathToServerRelativeUrl(string storagePath, Microsoft365Credentials _)
    {
        if (storagePath.StartsWith("sites/", StringComparison.OrdinalIgnoreCase))
        {
            var rootIndex = storagePath.IndexOf("root:", StringComparison.OrdinalIgnoreCase);
            if (rootIndex >= 0)
            {
                var relativePath = storagePath.Substring(rootIndex + 5);
                var sitePart = storagePath.Substring(0, rootIndex).TrimEnd('/');
                var libraryName = "Shared Documents";
                return $"/{sitePart}/{libraryName}{relativePath}";
            }
        }

        if (!storagePath.StartsWith('/'))
        {
            return "/" + storagePath;
        }

        return storagePath;
    }

    public Task SendTeamsMessageAsync(
        Guid tenantId,
        string teamId,
        string channelId,
        string message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "GraphService.SendTeamsMessageAsync called (stub) - TenantId: {TenantId}, TeamId: {TeamId}, ChannelId: {ChannelId}, MessageLength: {MessageLength}",
            tenantId,
            teamId,
            channelId,
            message?.Length ?? 0);

        // Phase 0: Stub implementation - no actual Graph API call
        return Task.CompletedTask;
    }

    public Task SendMailAsync(
        Guid tenantId,
        string to,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "GraphService.SendMailAsync called (stub) - TenantId: {TenantId}, To: {To}, Subject: {Subject}",
            tenantId,
            to,
            subject);

        // Phase 0: Stub implementation - no actual Graph API call
        return Task.CompletedTask;
    }

    public Task<UserProfileDto?> GetUserProfileAsync(
        Guid tenantId,
        string userPrincipalName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "GraphService.GetUserProfileAsync called (stub) - TenantId: {TenantId}, UserPrincipalName: {UserPrincipalName}",
            tenantId,
            userPrincipalName);

        // Phase 0: Stub implementation - return dummy data
        var dummyProfile = new UserProfileDto(
            DisplayName: $"Dummy User ({userPrincipalName})",
            UserPrincipalName: userPrincipalName,
            JobTitle: "Phase 0 Stub",
            Department: "Development"
        );

        return Task.FromResult<UserProfileDto?>(dummyProfile);
    }
}

