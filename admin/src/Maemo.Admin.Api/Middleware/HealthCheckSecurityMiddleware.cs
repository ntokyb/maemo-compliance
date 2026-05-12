using Maemo.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Maemo.Admin.Api.Middleware;

/// <summary>
/// Middleware to secure health check endpoints in GovOnPrem mode.
/// Optionally restricts access via API key or IP allowlist.
/// </summary>
public class HealthCheckSecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IDeploymentContext _deploymentContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HealthCheckSecurityMiddleware> _logger;
    private readonly HashSet<IPAddress>? _allowedIPs;
    private readonly string? _apiKey;

    public HealthCheckSecurityMiddleware(
        RequestDelegate next,
        IDeploymentContext deploymentContext,
        IConfiguration configuration,
        ILogger<HealthCheckSecurityMiddleware> logger)
    {
        _next = next;
        _deploymentContext = deploymentContext;
        _configuration = configuration;
        _logger = logger;

        // Only configure security in GovOnPrem mode
        if (_deploymentContext.IsGovOnPrem)
        {
            // Parse IP allowlist if configured
            var ipAllowlist = _configuration["HealthChecks:AllowedIPs"];
            if (!string.IsNullOrWhiteSpace(ipAllowlist))
            {
                _allowedIPs = new HashSet<IPAddress>();
                foreach (var ip in ipAllowlist.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    if (IPAddress.TryParse(ip, out var address))
                    {
                        _allowedIPs.Add(address);
                    }
                }
            }

            // Get API key if configured
            _apiKey = _configuration["HealthChecks:ApiKey"];
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only apply security to health check endpoints
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            // In GovOnPrem mode, apply security restrictions
            if (_deploymentContext.IsGovOnPrem)
            {
                var isAuthorized = false;

                // Check API key if configured
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    if (context.Request.Headers.TryGetValue("X-HealthCheck-ApiKey", out var providedKey))
                    {
                        if (providedKey == _apiKey)
                        {
                            isAuthorized = true;
                        }
                    }
                }

                // Check IP allowlist if configured
                if (!isAuthorized && _allowedIPs != null && _allowedIPs.Count > 0)
                {
                    var remoteIp = context.Connection.RemoteIpAddress;
                    if (remoteIp != null && _allowedIPs.Contains(remoteIp))
                    {
                        isAuthorized = true;
                    }
                }

                // If neither API key nor IP allowlist is configured, allow access (backward compatible)
                // But log access attempts for security monitoring
                if (!isAuthorized && (_allowedIPs == null || _allowedIPs.Count == 0) && string.IsNullOrWhiteSpace(_apiKey))
                {
                    isAuthorized = true;
                    _logger.LogInformation(
                        "Health check accessed from {RemoteIP} without authentication (no security configured)",
                        context.Connection.RemoteIpAddress);
                }

                if (!isAuthorized)
                {
                    _logger.LogWarning(
                        "Unauthorized health check access attempt from {RemoteIP}",
                        context.Connection.RemoteIpAddress);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Forbidden");
                    return;
                }
            }
        }

        await _next(context);
    }
}

