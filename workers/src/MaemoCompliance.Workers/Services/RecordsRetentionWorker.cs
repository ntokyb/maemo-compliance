using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Workers;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.Infrastructure.MultiTenancy;
using MaemoCompliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MaemoCompliance.Workers.Services;

/// <summary>
/// Background worker that checks documents past their retention period and marks them for archiving.
/// Runs daily to enforce NARSA (National Archives and Records Service) retention policies.
/// </summary>
public class RecordsRetentionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecordsRetentionWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24); // Run once per day
    private const string WorkerName = "RecordsRetentionWorker";

    public RecordsRetentionWorker(
        IServiceProvider serviceProvider,
        ILogger<RecordsRetentionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RecordsRetentionWorker started. Running every {Interval} hours.", _interval.TotalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            var startTime = DateTime.UtcNow;
            var status = "Success";
            string? errorMessage = null;

            try
            {
                using var scope = _serviceProvider.CreateScope();

                var dbContext = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                var tracker = scope.ServiceProvider.GetService<IWorkerExecutionTracker>();
                var applicationDbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var businessAuditLogger = scope.ServiceProvider.GetRequiredService<IBusinessAuditLogger>();

                var tenantId = tenantProvider.GetCurrentTenantId();
                var utcNow = dateTimeProvider.UtcNow;

                _logger.LogInformation("Running records retention worker for tenant {TenantId} at {UtcNow}", tenantId, utcNow);

                var markedCount = await ProcessDocumentsPastRetentionAsync(
                    dbContext,
                    tenantId,
                    utcNow,
                    businessAuditLogger,
                    stoppingToken);

                var duration = DateTime.UtcNow - startTime;
                tracker?.RecordExecution(WorkerName, startTime, status, duration, errorMessage);

                await PersistWorkerJobLog(applicationDbContext, tenantProvider, dateTimeProvider, WorkerName, startTime, duration, status, errorMessage);

                _logger.LogInformation(
                    "Records retention worker completed. Marked {Count} documents for archiving.",
                    markedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in RecordsRetentionWorker");
                status = "Failed";
                errorMessage = ex.Message;
                var duration = DateTime.UtcNow - startTime;

                using var errorScope = _serviceProvider.CreateScope();
                var errorTracker = errorScope.ServiceProvider.GetService<IWorkerExecutionTracker>();
                errorTracker?.RecordExecution(WorkerName, startTime, status, duration, errorMessage);

                var errorApplicationDbContext = errorScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var errorTenantProvider = errorScope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var errorDateTimeProvider = errorScope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                await PersistWorkerJobLog(errorApplicationDbContext, errorTenantProvider, errorDateTimeProvider, WorkerName, startTime, duration, status, errorMessage);
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task<int> ProcessDocumentsPastRetentionAsync(
        MaemoComplianceDbContext dbContext,
        Guid tenantId,
        DateTime utcNow,
        IBusinessAuditLogger businessAuditLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find documents past retention period that are not already archived or pending archive
            var documentsPastRetention = await dbContext.Documents
                .Where(d => d.TenantId == tenantId
                    && d.RetainUntil.HasValue
                    && d.RetainUntil.Value < utcNow
                    && !d.IsRetentionLocked
                    && d.WorkflowState != DocumentWorkflowState.Archived
                    && !d.IsPendingArchive
                    && d.IsCurrentVersion)
                .ToListAsync(cancellationToken);

            var markedCount = 0;

            foreach (var document in documentsPastRetention)
            {
                // Mark document as pending archive
                document.IsPendingArchive = true;
                document.ModifiedAt = utcNow;
                // Note: ModifiedBy is null for automated processes

                // Log business audit event
                await businessAuditLogger.LogAsync(
                    "Document.MarkedForArchive",
                    "Document",
                    document.Id.ToString(),
                    new
                    {
                        Title = document.Title,
                        RetainUntil = document.RetainUntil?.ToString("yyyy-MM-dd"),
                        DaysPastRetention = (utcNow - document.RetainUntil!.Value).Days,
                        Category = document.Category,
                        Department = document.Department,
                        WorkflowState = document.WorkflowState.ToString(),
                        MarkedAt = utcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    },
                    cancellationToken);

                markedCount++;

                _logger.LogInformation(
                    "Marked document {DocumentId} ({Title}) for archiving. Retention date: {RetainUntil}, Days past: {DaysPast}",
                    document.Id,
                    document.Title,
                    document.RetainUntil?.ToString("yyyy-MM-dd"),
                    (utcNow - document.RetainUntil!.Value).Days);
            }

            if (markedCount > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return markedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing documents past retention period");
            throw;
        }
    }

    private async Task PersistWorkerJobLog(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        string workerName,
        DateTime startTime,
        TimeSpan duration,
        string status,
        string? errorMessage)
    {
        // Worker execution logging - placeholder for now
        // Can be extended to persist to WorkerJobLogs table if needed
        await Task.CompletedTask;
    }
}

