using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Logging;
using MaemoCompliance.Infrastructure.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace MaemoCompliance.Engine.Api.Middleware;

/// <summary>
/// Middleware that logs unhandled exceptions for error tracking.
/// </summary>
public class ErrorLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorLoggingMiddleware> _logger;

    public ErrorLoggingMiddleware(RequestDelegate next, ILogger<ErrorLoggingMiddleware> logger)
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
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log error asynchronously (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    var tenantId = tenantContext.TenantId;
                    string? tenantName = null;

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

                    var log = new ErrorLog
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId ?? Guid.Empty,
                        TenantName = tenantName,
                        Message = ex.Message,
                        Level = "Error",
                        Timestamp = dateTimeProvider.UtcNow,
                        Source = context.Request.Path.Value ?? "Unknown",
                        CreatedAt = dateTimeProvider.UtcNow,
                        CreatedBy = "System"
                    };

                    // Add exception details as metadata
                    var metadata = new Dictionary<string, object?>
                    {
                        ["ExceptionType"] = ex.GetType().Name,
                        ["StackTrace"] = ex.StackTrace,
                        ["InnerException"] = ex.InnerException?.Message
                    };
                    log.MetadataJson = JsonSerializer.Serialize(metadata);

                    dbContext.ErrorLogs.Add(log);
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception logEx)
                {
                    // Don't let logging failures break the request
                    _logger.LogWarning(logEx, "Failed to log error");
                }
            });

            // Re-throw to let other middleware handle it
            throw;
        }
    }
}

