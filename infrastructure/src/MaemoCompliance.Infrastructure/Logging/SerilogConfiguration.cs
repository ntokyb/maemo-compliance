using MaemoCompliance.Application.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;

namespace MaemoCompliance.Infrastructure.Logging;

/// <summary>
/// Configures Serilog with security-focused settings for government deployments.
/// </summary>
public static class SerilogConfiguration
{
    public static LoggerConfiguration ConfigureSerilog(
        LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IServiceProvider? serviceProvider = null)
    {
        var deploymentMode = configuration["Deployment:Mode"] ?? "SaaS";
        var isGovOnPrem = string.Equals(deploymentMode, "GovOnPrem", StringComparison.OrdinalIgnoreCase);

        // Base configuration
        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning);

        // For GovOnPrem, reduce verbosity (avoid Debug level)
        if (isGovOnPrem)
        {
            loggerConfiguration
                .MinimumLevel.Information() // Ensure minimum is Information, not Debug
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information);
        }
        else
        {
            // For SaaS/Development, allow more verbose logging
            loggerConfiguration
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information);
        }

        // Filter out sensitive headers from HTTP request logging
        // Note: Header filtering is handled at the HTTP middleware level
        // This filter ensures sensitive property names are not logged
        loggerConfiguration
            .Filter.With(new SensitiveDataFilter());

        // Enrich with contextual information
        loggerConfiguration.Enrich.FromLogContext();

        // Add custom enrichers if service provider is available
        if (serviceProvider != null)
        {
            try
            {
                var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
                var tenantProvider = serviceProvider.GetService<ITenantProvider>();
                var currentUserService = serviceProvider.GetService<ICurrentUserService>();
                var deploymentContext = serviceProvider.GetService<IDeploymentContext>();

                if (httpContextAccessor != null || tenantProvider != null || 
                    currentUserService != null || deploymentContext != null)
                {
                    loggerConfiguration.Enrich.With(new LogEnricher(
                        httpContextAccessor,
                        tenantProvider,
                        currentUserService,
                        deploymentContext));
                }
            }
            catch
            {
                // If enrichment fails, continue without it
            }
        }

        // Configure console sink
        // Note: Message sanitization happens via enricher and property filtering
        loggerConfiguration.WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{TenantId}] [{UserId}] {Message:lj}{NewLine}{Exception}",
            formatProvider: null);

        // Configure file sink if path is provided (for GovOnPrem)
        var logPath = configuration["Serilog:WriteTo:File:Path"];
        if (!string.IsNullOrEmpty(logPath))
        {
            var rollingInterval = Enum.TryParse<RollingInterval>(
                configuration["Serilog:WriteTo:File:RollingInterval"] ?? "Day",
                out var interval) ? interval : RollingInterval.Day;

            loggerConfiguration.WriteTo.File(
                path: logPath,
                rollingInterval: rollingInterval,
                retainedFileCountLimit: isGovOnPrem ? 30 : null, // Keep 30 days for GovOnPrem
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] [{TenantId}] [{UserId}] {Message:lj} {Properties:j}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Information);
        }

        return loggerConfiguration;
    }
}

