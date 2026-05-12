using Maemo.Application.Common;
using Maemo.Application.Workers;
using Maemo.Domain.Logging;
using Maemo.Infrastructure.MultiTenancy;
using Maemo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Maemo.Workers.Services;

public class HeartbeatWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HeartbeatWorker> _logger;
    private const string WorkerName = "HeartbeatWorker";
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

    public HeartbeatWorker(
        IServiceProvider serviceProvider,
        ILogger<HeartbeatWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var startTime = DateTime.UtcNow;
            var status = "Success";
            string? errorMessage = null;

            try
            {
                // Create a scope for this iteration to resolve scoped services
                using var scope = _serviceProvider.CreateScope();
                
                // Resolve services from DI
                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                var dbContext = scope.ServiceProvider.GetRequiredService<MaemoDbContext>();
                var tracker = scope.ServiceProvider.GetService<IWorkerExecutionTracker>();
                var applicationDbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

                // Get current tenant ID
                var tenantId = tenantProvider.GetCurrentTenantId();
                var utcNow = dateTimeProvider.UtcNow;

                // Optionally query tenants table
                var tenantCount = await dbContext.Tenants.CountAsync(stoppingToken);

                // Log heartbeat message
                _logger.LogInformation(
                    "Maemo Heartbeat running for tenant {TenantId} at {UtcNow}. Total tenants in database: {TenantCount}",
                    tenantId,
                    utcNow,
                    tenantCount);

                var duration = DateTime.UtcNow - startTime;
                tracker?.RecordExecution(WorkerName, startTime, status, duration, errorMessage);
                
                // Persist to database
                await PersistWorkerJobLog(applicationDbContext, tenantProvider, dateTimeProvider, WorkerName, startTime, duration, status, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in HeartbeatWorker");
                status = "Failed";
                errorMessage = ex.Message;
                var duration = DateTime.UtcNow - startTime;
                
                // Create a new scope for error logging
                using var errorScope = _serviceProvider.CreateScope();
                var errorTracker = errorScope.ServiceProvider.GetService<IWorkerExecutionTracker>();
                errorTracker?.RecordExecution(WorkerName, startTime, status, duration, errorMessage);
                
                // Persist failure to database
                var errorApplicationDbContext = errorScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var errorTenantProvider = errorScope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var errorDateTimeProvider = errorScope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                await PersistWorkerJobLog(errorApplicationDbContext, errorTenantProvider, errorDateTimeProvider, WorkerName, startTime, duration, status, errorMessage);
            }

            // Wait 30 seconds before next iteration
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task PersistWorkerJobLog(
        IApplicationDbContext dbContext,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        string workerName,
        DateTime timestamp,
        TimeSpan duration,
        string status,
        string? errorMessage)
    {
        try
        {
            var tenantId = tenantProvider.GetCurrentTenantId();
            string? tenantName = null;

            // Get tenant name if tenant ID is not empty
            if (tenantId != Guid.Empty)
            {
                try
                {
                    var tenant = await dbContext.Tenants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Id == tenantId);
                    tenantName = tenant?.Name;
                }
                catch
                {
                    // If we can't get tenant name, continue without it
                }
            }

            var log = new WorkerJobLog
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TenantName = tenantName,
                WorkerName = workerName,
                Timestamp = timestamp,
                Duration = duration,
                Status = status,
                ErrorMessage = errorMessage,
                Source = WorkerName,
                CreatedAt = dateTimeProvider.UtcNow,
                CreatedBy = "System"
            };

            dbContext.WorkerJobLogs.Add(log);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Don't let logging failures break the worker
            _logger.LogWarning(ex, "Failed to persist worker job log");
        }
    }
}

