using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Logging;
using MaemoCompliance.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Text.Json;

namespace MaemoCompliance.Admin.Api.Middleware;

/// <summary>
/// Middleware that logs API calls for observability and analytics.
/// </summary>
public class ApiCallLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiCallLoggingMiddleware> _logger;

    public ApiCallLoggingMiddleware(RequestDelegate next, ILogger<ApiCallLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        IApplicationDbContext dbContext,
        IDateTimeProvider dateTimeProvider)
    {
        // Skip logging for health checks and admin endpoints (to reduce noise)
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var tenantId = tenantContext.TenantId;
        string? tenantName = null;

        // Capture all values from HttpContext BEFORE async operations
        // since the context will be disposed after the request completes
        var httpMethod = context.Request.Method;
        var requestPath = path;
        var routeController = context.Request.RouteValues["controller"]?.ToString() ?? "Endpoint";
        var userAgent = context.Request.Headers["User-Agent"].ToString();
        var contentType = context.Request.ContentType;
        var serviceScopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();

        // Get tenant name if tenant ID is available
        if (tenantId.HasValue)
        {
            try
            {
                var tenant = await dbContext.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == tenantId.Value);
                tenantName = tenant?.Name;
            }
            catch
            {
                // If we can't get tenant name, continue without it
            }
        }

        int statusCode = 200; // Default status code
        try
        {
            await _next(context);
            // Capture final status code after response
            statusCode = context.Response.StatusCode;
        }
        finally
        {
            stopwatch.Stop();
            var finalStatusCode = statusCode;
            var finalDuration = stopwatch.ElapsedMilliseconds;
            var capturedTenantId = tenantId;
            var capturedTenantName = tenantName;

            // Log API call asynchronously (fire and forget)
            // Use IServiceScopeFactory to create a new scope for background logging
            // since the request-scoped DbContext will be disposed after the request completes
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = serviceScopeFactory.CreateScope();
                    var scopedDbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                    var scopedDateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

                    var log = new ApiCallLog
                    {
                        Id = Guid.NewGuid(),
                        TenantId = capturedTenantId ?? Guid.Empty,
                        TenantName = capturedTenantName,
                        HttpMethod = httpMethod,
                        Path = requestPath,
                        StatusCode = finalStatusCode,
                        DurationMs = finalDuration,
                        Timestamp = scopedDateTimeProvider.UtcNow,
                        Source = routeController,
                        CreatedAt = scopedDateTimeProvider.UtcNow,
                        CreatedBy = "System"
                    };

                    // Add metadata if available
                    var metadata = new Dictionary<string, object?>
                    {
                        ["UserAgent"] = userAgent,
                        ["ContentType"] = contentType
                    };
                    log.MetadataJson = JsonSerializer.Serialize(metadata);

                    scopedDbContext.ApiCallLogs.Add(log);
                    await scopedDbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Don't let logging failures break the request
                    _logger.LogWarning(ex, "Failed to log API call");
                }
            });
        }
    }
}

