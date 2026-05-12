namespace Maemo.Application.Common;

public sealed record SharePointConnectionTestResult(bool Success, string Message, string? LibraryUrl);

/// <summary>
/// Tests SharePoint / Microsoft Graph connectivity using app-only credentials.
/// </summary>
public interface ISharePointConnectionTester
{
    Task<SharePointConnectionTestResult> TestConnectionAsync(
        string azureTenantId,
        string clientId,
        string clientSecretPlain,
        string siteUrl,
        string libraryName,
        CancellationToken cancellationToken = default);
}
