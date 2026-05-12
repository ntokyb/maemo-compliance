using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Tenants.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Tenants.Commands;

public class ConnectMicrosoft365CommandHandler : IRequestHandler<ConnectMicrosoft365Command>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDeploymentContext _deploymentContext;
    private readonly IEncryptionService? _encryptionService;

    public ConnectMicrosoft365CommandHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IDeploymentContext deploymentContext,
        IEncryptionService? encryptionService = null)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _deploymentContext = deploymentContext;
        _encryptionService = encryptionService;
    }

    public async Task Handle(ConnectMicrosoft365Command command, CancellationToken cancellationToken)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == command.TenantId, cancellationToken);

        if (tenant == null)
        {
            throw new KeyNotFoundException($"Tenant with Id {command.TenantId} not found.");
        }

        var request = command.Request;

        // Validate required fields
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            throw new ArgumentException("ClientId is required.", nameof(request.ClientId));
        }

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            throw new ArgumentException("ClientSecret is required.", nameof(request.ClientSecret));
        }

        if (string.IsNullOrWhiteSpace(request.TenantId))
        {
            throw new ArgumentException("TenantId is required.", nameof(request.TenantId));
        }

        // Update tenant with Microsoft 365 credentials
        tenant.AzureAdTenantId = request.TenantId;
        tenant.AzureAdClientId = request.ClientId;
        
        // Encrypt client secret in GovOnPrem mode
        if (_deploymentContext.IsGovOnPrem)
        {
            if (_encryptionService == null)
            {
                throw new InvalidOperationException(
                    "Encryption service is required for GovOnPrem mode but is not configured. " +
                    "Please configure Security:EncryptionKey in appsettings.");
            }
            tenant.AzureAdClientSecret = _encryptionService.Encrypt(request.ClientSecret);
        }
        else
        {
            // SaaS mode - store unencrypted (will be encrypted by cloud provider)
            tenant.AzureAdClientSecret = request.ClientSecret;
        }
        
        tenant.SharePointSiteId = request.SharePointSiteId;
        tenant.SharePointDriveId = request.SharePointDriveId;
        tenant.ModifiedAt = _dateTimeProvider.UtcNow;
        tenant.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

