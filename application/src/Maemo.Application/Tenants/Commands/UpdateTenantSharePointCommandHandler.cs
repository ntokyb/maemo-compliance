using Maemo.Application.Common;
using Maemo.Application.Tenants;
using Maemo.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Tenants.Commands;

public class UpdateTenantSharePointCommandHandler : IRequestHandler<UpdateTenantSharePointCommand, TenantSharePointSettingsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDeploymentContext _deploymentContext;
    private readonly IEncryptionService? _encryptionService;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UpdateTenantSharePointCommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IDeploymentContext deploymentContext,
        IBusinessAuditLogger businessAuditLogger,
        IEncryptionService? encryptionService = null)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _deploymentContext = deploymentContext;
        _businessAuditLogger = businessAuditLogger;
        _encryptionService = encryptionService;
    }

    public async Task<TenantSharePointSettingsDto> Handle(UpdateTenantSharePointCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with Id {command.TenantId} not found.");
        }

        if (command.SharePointSiteUrl is not null)
        {
            if (string.IsNullOrWhiteSpace(command.SharePointSiteUrl))
            {
                tenant.SharePointSiteUrl = null;
            }
            else
            {
                if (!Uri.TryCreate(command.SharePointSiteUrl.Trim(), UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                {
                    throw new ArgumentException("SharePoint site URL must be a valid http(s) URL.");
                }

                tenant.SharePointSiteUrl = command.SharePointSiteUrl.Trim();
            }
        }

        if (command.SharePointClientId is not null)
        {
            tenant.SharePointClientId = string.IsNullOrWhiteSpace(command.SharePointClientId)
                ? null
                : command.SharePointClientId.Trim();
        }

        if (command.SharePointLibraryName is not null)
        {
            tenant.SharePointLibraryName = string.IsNullOrWhiteSpace(command.SharePointLibraryName)
                ? "Shared Documents"
                : command.SharePointLibraryName.Trim();
        }

        if (!string.IsNullOrEmpty(command.SharePointClientSecret))
        {
            tenant.SharePointClientSecretEncrypted = TenantSecretProtector.ProtectSecret(
                command.SharePointClientSecret,
                _deploymentContext,
                _encryptionService);
        }

        tenant.ModifiedAt = _dateTimeProvider.UtcNow;
        tenant.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        await _businessAuditLogger.LogAsync(
            "Tenant.SharePointConfigured",
            "Tenant",
            tenant.Id.ToString(),
            new { tenant.SharePointSiteUrl, tenant.SharePointLibraryName, HasSecret = !string.IsNullOrEmpty(tenant.SharePointClientSecretEncrypted) },
            cancellationToken);

        return new TenantSharePointSettingsDto
        {
            SharePointSiteUrl = tenant.SharePointSiteUrl,
            SharePointLibraryName = tenant.SharePointLibraryName,
            SharePointClientId = tenant.SharePointClientId,
            ClientSecretMasked = string.IsNullOrEmpty(tenant.SharePointClientSecretEncrypted) ? "" : "••••••"
        };
    }
}
