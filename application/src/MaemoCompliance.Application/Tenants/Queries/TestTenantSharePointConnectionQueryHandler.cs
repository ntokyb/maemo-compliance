using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Tenants.Queries;

public class TestTenantSharePointConnectionQueryHandler : IRequestHandler<TestTenantSharePointConnectionQuery, SharePointConnectionTestResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ISharePointConnectionTester _tester;
    private readonly IDeploymentContext _deploymentContext;
    private readonly IEncryptionService? _encryptionService;

    public TestTenantSharePointConnectionQueryHandler(
        IApplicationDbContext context,
        ISharePointConnectionTester tester,
        IDeploymentContext deploymentContext,
        IEncryptionService? encryptionService = null)
    {
        _context = context;
        _tester = tester;
        _deploymentContext = deploymentContext;
        _encryptionService = encryptionService;
    }

    public async Task<SharePointConnectionTestResult> Handle(TestTenantSharePointConnectionQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with Id {request.TenantId} not found.");
        }

        var azureTenantId = tenant.AzureAdTenantId;
        var clientId = !string.IsNullOrWhiteSpace(tenant.SharePointClientId)
            ? tenant.SharePointClientId
            : tenant.AzureAdClientId;

        string? secretPlain = null;
        if (!string.IsNullOrEmpty(tenant.SharePointClientSecretEncrypted))
        {
            secretPlain = TenantSecretProtector.UnprotectSecret(
                tenant.SharePointClientSecretEncrypted,
                _deploymentContext,
                _encryptionService);
        }
        else if (!string.IsNullOrEmpty(tenant.AzureAdClientSecret))
        {
            secretPlain = TenantSecretProtector.UnprotectSecret(
                tenant.AzureAdClientSecret,
                _deploymentContext,
                _encryptionService);
        }

        if (string.IsNullOrWhiteSpace(azureTenantId) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(secretPlain))
        {
            throw new InvalidOperationException(
                "Azure AD tenant ID, app client ID, and a configured client secret are required. Save M365 or SharePoint credentials first.");
        }

        var siteUrl = tenant.SharePointSiteUrl;
        if (string.IsNullOrWhiteSpace(siteUrl))
        {
            throw new InvalidOperationException("SharePoint site URL is not configured.");
        }

        var library = string.IsNullOrWhiteSpace(tenant.SharePointLibraryName)
            ? "Shared Documents"
            : tenant.SharePointLibraryName;

        return await _tester.TestConnectionAsync(
            azureTenantId,
            clientId,
            secretPlain,
            siteUrl,
            library,
            cancellationToken);
    }
}
