using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MaemoCompliance.Application.Tenants.Queries;

public class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDeploymentContext _deploymentContext;
    private readonly IEncryptionService? _encryptionService;

    public GetTenantByIdQueryHandler(
        IApplicationDbContext context,
        IDeploymentContext deploymentContext,
        IEncryptionService? encryptionService = null)
    {
        _context = context;
        _deploymentContext = deploymentContext;
        _encryptionService = encryptionService;
    }

    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with Id {request.Id} not found.");
        }

        // Decrypt sensitive fields in GovOnPrem mode
        string? decryptedClientSecret = DecryptIfNeeded(tenant.AzureAdClientSecret);

        // Parse modules enabled JSON
        string[]? modulesEnabled = null;
        if (!string.IsNullOrWhiteSpace(tenant.ModulesEnabledJson))
        {
            try
            {
                var modules = JsonSerializer.Deserialize<string[]>(tenant.ModulesEnabledJson);
                modulesEnabled = modules;
            }
            catch
            {
                // If JSON is invalid, leave as null
                modulesEnabled = null;
            }
        }

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Domain = tenant.Domain,
            AdminEmail = tenant.AdminEmail,
            IsActive = tenant.IsActive,
            Plan = tenant.Plan,
            SubscriptionId = DecryptIfNeeded(tenant.SubscriptionId),
            CreatedAt = tenant.CreatedAt,
            TrialEndsAt = tenant.TrialEndsAt,
            ModulesEnabled = modulesEnabled,
            LogoUrl = tenant.LogoUrl,
            PrimaryColor = tenant.PrimaryColor,
            AzureAdTenantId = tenant.AzureAdTenantId,
            AzureAdClientId = tenant.AzureAdClientId,
            AzureAdClientSecret = decryptedClientSecret,
            SharePointSiteId = tenant.SharePointSiteId,
            SharePointDriveId = tenant.SharePointDriveId,
            Edition = tenant.Edition,
            LicenseExpiryDate = tenant.LicenseExpiryDate,
            MaxUsers = tenant.MaxUsers,
            MaxStorageBytes = tenant.MaxStorageBytes,
            SharePointSiteUrl = tenant.SharePointSiteUrl,
            SharePointLibraryName = tenant.SharePointLibraryName,
            SharePointClientId = tenant.SharePointClientId,
            SharePointClientSecretConfigured = !string.IsNullOrEmpty(tenant.SharePointClientSecretEncrypted)
        };
    }

    private string? DecryptIfNeeded(string? encryptedValue)
    {
        if (string.IsNullOrEmpty(encryptedValue))
        {
            return encryptedValue;
        }

        if (_deploymentContext.IsGovOnPrem)
        {
            if (_encryptionService == null)
            {
                throw new InvalidOperationException(
                    "Encryption service is required for GovOnPrem mode but is not configured. " +
                    "Please configure Security:EncryptionKey in appsettings.");
            }
            try
            {
                return _encryptionService.Decrypt(encryptedValue);
            }
            catch (Exception ex)
            {
                // Log error but don't fail - might be legacy unencrypted data
                // In production, you might want to handle this differently
                throw new InvalidOperationException(
                    "Failed to decrypt sensitive field. The data may be corrupted or encrypted with a different key.", ex);
            }
        }
        else
        {
            // SaaS mode - return as-is
            return encryptedValue;
        }
    }
}

