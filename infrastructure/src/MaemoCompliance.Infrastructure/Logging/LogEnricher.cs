using MaemoCompliance.Application.Common;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace MaemoCompliance.Infrastructure.Logging;

/// <summary>
/// Enriches log events with contextual information (TenantId, UserId, DeploymentMode).
/// </summary>
public class LogEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly ITenantProvider? _tenantProvider;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IDeploymentContext? _deploymentContext;

    public LogEnricher(
        IHttpContextAccessor? httpContextAccessor = null,
        ITenantProvider? tenantProvider = null,
        ICurrentUserService? currentUserService = null,
        IDeploymentContext? deploymentContext = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantProvider = tenantProvider;
        _currentUserService = currentUserService;
        _deploymentContext = deploymentContext;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        // Add TenantId
        if (_tenantProvider != null)
        {
            try
            {
                var tenantId = _tenantProvider.GetCurrentTenantId();
                if (tenantId != Guid.Empty)
                {
                    logEvent.AddPropertyIfAbsent(
                        propertyFactory.CreateProperty("TenantId", tenantId.ToString()));
                }
            }
            catch
            {
                // Ignore errors - tenant context may not be available in all scenarios
            }
        }

        // Add UserId
        if (_currentUserService != null)
        {
            try
            {
                var userId = _currentUserService.UserId;
                if (!string.IsNullOrEmpty(userId))
                {
                    logEvent.AddPropertyIfAbsent(
                        propertyFactory.CreateProperty("UserId", userId));
                }
            }
            catch
            {
                // Ignore errors - user context may not be available in all scenarios
            }
        }

        // Add DeploymentMode
        if (_deploymentContext != null)
        {
            try
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("DeploymentMode", _deploymentContext.Mode.ToString()));
            }
            catch
            {
                // Ignore errors
            }
        }

        // Add RequestId from HTTP context if available
        if (_httpContextAccessor?.HttpContext != null)
        {
            var requestId = _httpContextAccessor.HttpContext.TraceIdentifier;
            if (!string.IsNullOrEmpty(requestId))
            {
                logEvent.AddPropertyIfAbsent(
                    propertyFactory.CreateProperty("RequestId", requestId));
            }
        }
    }
}

