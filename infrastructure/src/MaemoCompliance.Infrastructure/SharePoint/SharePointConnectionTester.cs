using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using MaemoCompliance.Application.Common;
using Microsoft.Extensions.Logging;

namespace MaemoCompliance.Infrastructure.SharePoint;

public class SharePointConnectionTester : ISharePointConnectionTester
{
    private readonly ILogger<SharePointConnectionTester> _logger;

    public SharePointConnectionTester(ILogger<SharePointConnectionTester> logger)
    {
        _logger = logger;
    }

    public async Task<SharePointConnectionTestResult> TestConnectionAsync(
        string azureTenantId,
        string clientId,
        string clientSecretPlain,
        string siteUrl,
        string libraryName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(azureTenantId) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecretPlain) ||
            string.IsNullOrWhiteSpace(siteUrl))
        {
            return new SharePointConnectionTestResult(false, "Tenant ID, client ID, client secret, and site URL are required.", null);
        }

        libraryName = string.IsNullOrWhiteSpace(libraryName) ? "Shared Documents" : libraryName.Trim();

        if (!Uri.TryCreate(siteUrl.Trim(), UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new SharePointConnectionTestResult(false, "SharePoint site URL must be a valid http(s) URL.", null);
        }

        string accessToken;
        try
        {
            var credential = new ClientSecretCredential(azureTenantId, clientId, clientSecretPlain);
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(["https://graph.microsoft.com/.default"]),
                cancellationToken);
            accessToken = token.Token;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SharePoint test: token acquisition failed");
            return new SharePointConnectionTestResult(false,
                $"Could not acquire Microsoft Graph token. Check tenant ID, client ID, and secret. ({ex.Message})",
                null);
        }

        var pathPart = uri.AbsolutePath.TrimStart('/');
        if (string.IsNullOrEmpty(pathPart))
        {
            return new SharePointConnectionTestResult(false,
                "Use a full site path (e.g. https://tenant.sharepoint.com/sites/YourSite).",
                null);
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var siteRequestUrl = $"https://graph.microsoft.com/v1.0/sites/{uri.Host}:/{pathPart}";

        HttpResponseMessage siteResponse;
        try
        {
            siteResponse = await http.GetAsync(siteRequestUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SharePoint test: Graph site request failed");
            return new SharePointConnectionTestResult(false,
                $"Network error calling Microsoft Graph: {ex.Message}",
                null);
        }

        if (!siteResponse.IsSuccessStatusCode)
        {
            var body = await siteResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("SharePoint test: Graph returned {Status}. Body: {Body}", siteResponse.StatusCode, body);
            return new SharePointConnectionTestResult(false,
                $"Microsoft Graph site lookup failed ({(int)siteResponse.StatusCode}). Ensure the app has Sites.Read.All or AllSites.Read and the URL is correct.",
                null);
        }

        using var siteDoc = JsonDocument.Parse(await siteResponse.Content.ReadAsStringAsync(cancellationToken));
        if (!siteDoc.RootElement.TryGetProperty("id", out var idProp))
        {
            return new SharePointConnectionTestResult(false, "Microsoft Graph returned an unexpected site payload.", null);
        }

        var graphSiteId = idProp.GetString();
        if (string.IsNullOrEmpty(graphSiteId))
        {
            return new SharePointConnectionTestResult(false, "Could not read site ID from Microsoft Graph.", null);
        }

        var encSiteId = Uri.EscapeDataString(graphSiteId);
        var listsUrl =
            $"https://graph.microsoft.com/v1.0/sites/{encSiteId}/lists?" +
            "$filter=" + Uri.EscapeDataString($"name eq '{libraryName.Replace("'", "''", StringComparison.Ordinal)}'");

        HttpResponseMessage listsResponse;
        try
        {
            listsResponse = await http.GetAsync(listsUrl, cancellationToken);
        }
        catch (Exception ex)
        {
            return new SharePointConnectionTestResult(false, $"Failed to query document libraries: {ex.Message}", null);
        }

        if (!listsResponse.IsSuccessStatusCode)
        {
            var lb = await listsResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("SharePoint test: lists failed {Status} {Body}", listsResponse.StatusCode, lb);
            return new SharePointConnectionTestResult(false,
                $"Could not verify library \"{libraryName}\". Ensure Sites.Read.All / Files.Read.All permissions. ({(int)listsResponse.StatusCode})",
                siteUrl.TrimEnd('/'));
        }

        using var listsDoc = JsonDocument.Parse(await listsResponse.Content.ReadAsStringAsync(cancellationToken));
        if (!listsDoc.RootElement.TryGetProperty("value", out var value) || value.GetArrayLength() == 0)
        {
            return new SharePointConnectionTestResult(false,
                $"Library \"{libraryName}\" was not found on this site.",
                siteUrl.TrimEnd('/'));
        }

        var webUrl = siteUrl.TrimEnd('/');
        if (value[0].TryGetProperty("webUrl", out var wu))
        {
            webUrl = wu.GetString() ?? webUrl;
        }

        return new SharePointConnectionTestResult(true,
            $"Connected. Library \"{libraryName}\" is reachable.",
            webUrl);
    }
}
