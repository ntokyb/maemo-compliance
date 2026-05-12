using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Workers;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.Infrastructure.MultiTenancy;
using MaemoCompliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MaemoCompliance.Domain.Logging;

namespace MaemoCompliance.Workers.Services;

/// <summary>
/// Background worker that physically deletes files for documents marked as destroyed.
/// Only runs if PhysicalDeletionEnabled is true in configuration.
/// </summary>
public class DocumentDestructionWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DocumentDestructionWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24); // Run once per day
    private const string WorkerName = "DocumentDestructionWorker";
    private const int GracePeriodDays = 30; // Grace period before physical deletion

    public DocumentDestructionWorker(
        IServiceProvider serviceProvider,
        ILogger<DocumentDestructionWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DocumentDestructionWorker started. Running every {Interval} hours.", _interval.TotalHours);

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
                var fileStorageProvider = scope.ServiceProvider.GetRequiredService<MaemoCompliance.Application.Common.IFileStorageProvider>();

                // Check if physical deletion is enabled (optional feature)
                var configuration = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var physicalDeletionEnabled = configuration.GetValue<bool>("Retention:PhysicalDeletionEnabled", false);

                if (!physicalDeletionEnabled)
                {
                    _logger.LogInformation("Physical deletion is disabled. Skipping document destruction worker.");
                    await Task.Delay(_interval, stoppingToken);
                    continue;
                }

                var tenantId = tenantProvider.GetCurrentTenantId();
                var utcNow = dateTimeProvider.UtcNow;
                var gracePeriodDate = utcNow.AddDays(-GracePeriodDays);

                _logger.LogInformation("Running document destruction worker for tenant {TenantId} at {UtcNow}", tenantId, utcNow);

                await ProcessDestroyedDocumentsAsync(
                    dbContext,
                    tenantId,
                    gracePeriodDate,
                    fileStorageProvider,
                    businessAuditLogger,
                    stoppingToken);

                var duration = DateTime.UtcNow - startTime;
                tracker?.RecordExecution(WorkerName, startTime, status, duration, errorMessage);

                await PersistWorkerJobLog(applicationDbContext, tenantProvider, dateTimeProvider, WorkerName, startTime, duration, status, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in DocumentDestructionWorker");
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

    private async Task ProcessDestroyedDocumentsAsync(
        MaemoComplianceDbContext dbContext,
        Guid tenantId,
        DateTime gracePeriodDate,
        MaemoCompliance.Application.Common.IFileStorageProvider fileStorageProvider,
        IBusinessAuditLogger businessAuditLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            // Find documents destroyed before the grace period date
            var destroyedDocuments = await dbContext.Documents
                .IgnoreQueryFilters() // Include destroyed documents
                .Where(d => d.TenantId == tenantId
                    && d.IsDestroyed
                    && d.DestroyedAt.HasValue
                    && d.DestroyedAt.Value < gracePeriodDate)
                .ToListAsync(cancellationToken);

            foreach (var document in destroyedDocuments)
            {
                var deletedFiles = new System.Collections.Generic.List<string>();

                // Delete document versions
                var versions = await dbContext.DocumentVersions
                    .Where(dv => dv.DocumentId == document.Id)
                    .ToListAsync(cancellationToken);

                foreach (var version in versions)
                {
                    if (!string.IsNullOrWhiteSpace(version.StorageLocation))
                    {
                        try
                        {
                            await fileStorageProvider.DeleteAsync(tenantId, version.StorageLocation, cancellationToken);
                            deletedFiles.Add(version.StorageLocation);
                            _logger.LogInformation(
                                "Physically deleted version {VersionNumber} file for destroyed document {DocumentId} - {StorageLocation}",
                                version.VersionNumber,
                                document.Id,
                                version.StorageLocation);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "Failed to physically delete version {VersionNumber} file for destroyed document {DocumentId} - {StorageLocation}",
                                version.VersionNumber,
                                document.Id,
                                version.StorageLocation);
                        }
                    }
                }

                // Delete main document file if different from versions
                if (!string.IsNullOrWhiteSpace(document.StorageLocation) &&
                    !versions.Any(v => v.StorageLocation == document.StorageLocation))
                {
                    try
                    {
                        await fileStorageProvider.DeleteAsync(tenantId, document.StorageLocation, cancellationToken);
                        deletedFiles.Add(document.StorageLocation);
                        _logger.LogInformation(
                            "Physically deleted main file for destroyed document {DocumentId} - {StorageLocation}",
                            document.Id,
                            document.StorageLocation);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Failed to physically delete main file for destroyed document {DocumentId} - {StorageLocation}",
                            document.Id,
                            document.StorageLocation);
                    }
                }

                // Log physical deletion
                if (deletedFiles.Any())
                {
                    await businessAuditLogger.LogAsync(
                        "Document.FilesPhysicallyDeleted",
                        "Document",
                        document.Id.ToString(),
                        new
                        {
                            Title = document.Title,
                            DestroyedAt = document.DestroyedAt?.ToString("yyyy-MM-dd HH:mm:ss"),
                            DestroyedBy = document.DestroyedByUserId,
                            DeletedFiles = deletedFiles,
                            DeletedFileCount = deletedFiles.Count
                        },
                        cancellationToken);
                }
            }

            _logger.LogInformation(
                "Processed {Count} destroyed documents for physical file deletion",
                destroyedDocuments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing destroyed documents for physical deletion");
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

