using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.AuditLog;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace MaemoCompliance.Infrastructure.AuditLog;

/// <summary>
/// Implementation of business audit logger.
/// Records semantic business events for compliance traceability.
/// </summary>
public class BusinessAuditLogger : IBusinessAuditLogger
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<BusinessAuditLogger> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BusinessAuditLogger(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        ILogger<BusinessAuditLogger> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task LogAsync(
        string action,
        string entityType,
        string entityId,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        return LogWithTenantAsync(tenantId, action, entityType, entityId, metadata, cancellationToken);
    }

    public Task LogForTenantAsync(
        Guid tenantId,
        string action,
        string entityType,
        string entityId,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        return LogWithTenantAsync(tenantId, action, entityType, entityId, metadata, cancellationToken);
    }

    private async Task LogWithTenantAsync(
        Guid tenantId,
        string action,
        string entityType,
        string entityId,
        object? metadata,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId ?? "System";
            var username = userId;
            var user = _httpContextAccessor.HttpContext?.User;
            if (user != null)
            {
                username = user.FindFirstValue(ClaimTypes.Name)
                    ?? user.FindFirstValue("name")
                    ?? user.FindFirstValue(ClaimTypes.Email)
                    ?? user.FindFirstValue("email")
                    ?? userId;
            }

            string? metadataJson = null;
            if (metadata != null)
            {
                try
                {
                    metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to serialize metadata for audit log: {Action}", action);
                }
            }

            var auditLog = new BusinessAuditLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Username = username,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Timestamp = _dateTimeProvider.UtcNow,
                MetadataJson = metadataJson,
                CreatedAt = _dateTimeProvider.UtcNow,
                CreatedBy = userId
            };

            _context.BusinessAuditLogs.Add(auditLog);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug(
                "Business audit log created: {Action} on {EntityType}/{EntityId} by {Username}",
                action,
                entityType,
                entityId,
                username);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create business audit log: {Action} on {EntityType}/{EntityId}",
                action,
                entityType,
                entityId);
        }
    }
}

