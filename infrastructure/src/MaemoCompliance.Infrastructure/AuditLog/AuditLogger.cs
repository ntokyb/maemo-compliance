using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.AuditLog;
using MaemoCompliance.Infrastructure.Persistence;
using System.Text.Json;

namespace MaemoCompliance.Infrastructure.AuditLog;

public class AuditLogger : IAuditLogger
{
    private readonly MaemoComplianceDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AuditLogger(
        MaemoComplianceDbContext context,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task LogAsync(
        string action,
        string? entityType = null,
        Guid? entityId = null,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action cannot be null or empty", nameof(action));
        }

        var tenantId = _tenantProvider.GetCurrentTenantId();
        if (tenantId == Guid.Empty)
        {
            // Don't log if no tenant context (shouldn't happen in normal flow)
            return;
        }

        string? metadataJson = null;
        if (metadata != null)
        {
            try
            {
                metadataJson = JsonSerializer.Serialize(metadata, _jsonOptions);
            }
            catch (Exception)
            {
                // If serialization fails, log without metadata rather than failing the operation
                metadataJson = null;
            }
        }

        var auditEntry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            PerformedByUserId = _currentUserService.UserId,
            PerformedAt = _dateTimeProvider.UtcNow,
            MetadataJson = metadataJson,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.UserId
        };

        _context.AuditLogs.Add(auditEntry);
        
        // Save immediately to ensure audit trail is persisted
        // This is important for compliance - audit logs should be written synchronously
        await _context.SaveChangesAsync(cancellationToken);
    }
}

