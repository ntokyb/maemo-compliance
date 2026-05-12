using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Workers;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.Domain.Logging;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.Domain.Risks;
using MaemoCompliance.Infrastructure.MultiTenancy;
using MaemoCompliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MaemoCompliance.Workers.Services;

public class ComplianceJobsWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComplianceJobsWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // 5 minutes in development
    private const string WorkerName = "ComplianceJobsWorker";

    public ComplianceJobsWorker(
        IServiceProvider serviceProvider,
        ILogger<ComplianceJobsWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ComplianceJobsWorker started. Running jobs every {Interval} minutes.", _interval.TotalMinutes);

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
                var dbContext = scope.ServiceProvider.GetRequiredService<MaemoComplianceDbContext>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                var tracker = scope.ServiceProvider.GetService<IWorkerExecutionTracker>();
                var applicationDbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var businessAuditLogger = scope.ServiceProvider.GetRequiredService<IBusinessAuditLogger>();

                // Get current tenant ID and UTC date
                var tenantId = tenantProvider.GetCurrentTenantId();
                var today = dateTimeProvider.UtcNow.Date;
                var reviewDateThreshold = today.AddDays(14);

                _logger.LogInformation("Running compliance jobs for tenant {TenantId} at {UtcNow}", tenantId, dateTimeProvider.UtcNow);

                // Job 1: Document review reminders
                await ProcessDocumentReviewRemindersAsync(dbContext, tenantId, today, reviewDateThreshold, stoppingToken);

                // Job 2: Overdue NCR detection
                await ProcessOverdueNcrsAsync(dbContext, tenantId, today, stoppingToken);

                // Job 3: High residual risk logging
                await ProcessHighResidualRisksAsync(dbContext, tenantId, stoppingToken);

                // Job 4: Critical NCR logging
                await ProcessCriticalNcrsAsync(dbContext, tenantId, stoppingToken);

                // Job 5: Retention upcoming and expired detection
                await ProcessRetentionUpcomingAsync(dbContext, tenantId, dateTimeProvider, businessAuditLogger, stoppingToken);
                await ProcessRetentionExpiredAsync(dbContext, tenantId, dateTimeProvider, businessAuditLogger, stoppingToken);

                var duration = DateTime.UtcNow - startTime;
                tracker?.RecordExecution(WorkerName, startTime, status, duration, errorMessage);
                
                // Persist to database
                await PersistWorkerJobLog(applicationDbContext, tenantProvider, dateTimeProvider, WorkerName, startTime, duration, status, errorMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in ComplianceJobsWorker");
                status = "Failed";
                errorMessage = ex.Message;
                var duration = DateTime.UtcNow - startTime;
                
                var tracker = _serviceProvider.CreateScope().ServiceProvider.GetService<IWorkerExecutionTracker>();
                tracker?.RecordExecution(WorkerName, startTime, status, duration, errorMessage);
                
                // Persist failure to database
                using var scope = _serviceProvider.CreateScope();
                var applicationDbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
                var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
                await PersistWorkerJobLog(applicationDbContext, tenantProvider, dateTimeProvider, WorkerName, startTime, duration, status, errorMessage);
            }

            // Wait for the interval before next iteration
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessDocumentReviewRemindersAsync(
        MaemoComplianceDbContext dbContext,
        Guid tenantId,
        DateTime today,
        DateTime reviewDateThreshold,
        CancellationToken cancellationToken)
    {
        try
        {
            var upcomingReviews = await dbContext.Documents
                .Where(d => d.TenantId == tenantId
                    && d.Status == DocumentStatus.Active
                    && d.ReviewDate.Date >= today
                    && d.ReviewDate.Date <= reviewDateThreshold)
                .ToListAsync(cancellationToken);

            if (upcomingReviews.Any())
            {
                _logger.LogInformation("Found {Count} document(s) with upcoming reviews", upcomingReviews.Count);

                foreach (var document in upcomingReviews)
                {
                    _logger.LogInformation(
                        "Upcoming document review: {Title} (Id: {Id}) due on {ReviewDate:yyyy-MM-dd} for tenant {TenantId}",
                        document.Title,
                        document.Id,
                        document.ReviewDate,
                        tenantId);
                }
            }
            else
            {
                _logger.LogDebug("No documents with upcoming reviews found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document review reminders");
        }
    }

    private async Task ProcessOverdueNcrsAsync(
        MaemoComplianceDbContext dbContext,
        Guid tenantId,
        DateTime today,
        CancellationToken cancellationToken)
    {
        try
        {
            var overdueNcrs = await dbContext.Ncrs
                .Where(n => n.TenantId == tenantId
                    && n.Status != NcrStatus.Closed
                    && n.DueDate.HasValue
                    && n.DueDate.Value.Date < today)
                .ToListAsync(cancellationToken);

            if (overdueNcrs.Any())
            {
                _logger.LogWarning("Found {Count} overdue NCR(s)", overdueNcrs.Count);

                foreach (var ncr in overdueNcrs)
                {
                    _logger.LogWarning(
                        "Overdue NCR: {Title} (Id: {Id}) due on {DueDate:yyyy-MM-dd} for tenant {TenantId}",
                        ncr.Title,
                        ncr.Id,
                        ncr.DueDate!.Value,
                        tenantId);
                }
            }
            else
            {
                _logger.LogDebug("No overdue NCRs found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing overdue NCRs");
        }
    }

    private async Task ProcessHighResidualRisksAsync(
        MaemoComplianceDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            const int highRiskThreshold = 15; // ResidualScore >= 15 is considered High

            var highRisks = await dbContext.Risks
                .Where(r => r.TenantId == tenantId
                    && r.ResidualScore >= highRiskThreshold)
                .ToListAsync(cancellationToken);

            if (highRisks.Any())
            {
                _logger.LogWarning("Found {Count} high residual risk(s)", highRisks.Count);

                foreach (var risk in highRisks)
                {
                    _logger.LogWarning(
                        "High risk detected: {Title} (Id: {Id}) residual score {ResidualScore} for tenant {TenantId}",
                        risk.Title,
                        risk.Id,
                        risk.ResidualScore,
                        tenantId);
                }
            }
            else
            {
                _logger.LogDebug("No high residual risks found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing high residual risks");
        }
    }

    private async Task ProcessCriticalNcrsAsync(
        MaemoComplianceDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        try
        {
            var criticalNcrs = await dbContext.Ncrs
                .Where(n => n.TenantId == tenantId
                    && n.Status != NcrStatus.Closed
                    && n.Severity == NcrSeverity.High)
                .ToListAsync(cancellationToken);

            if (criticalNcrs.Any())
            {
                _logger.LogWarning("Found {Count} high severity open NCR(s)", criticalNcrs.Count);

                foreach (var ncr in criticalNcrs)
                {
                    var dueDateStr = ncr.DueDate.HasValue 
                        ? ncr.DueDate.Value.ToString("yyyy-MM-dd") 
                        : "not set";

                    _logger.LogWarning(
                        "High severity NCR open: {Title} (Id: {Id}) severity {Severity} due {DueDate} for tenant {TenantId}",
                        ncr.Title,
                        ncr.Id,
                        ncr.Severity,
                        dueDateStr,
                        tenantId);
                }
            }
            else
            {
                _logger.LogDebug("No high severity open NCRs found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing critical NCRs");
        }
    }

    private async Task ProcessRetentionUpcomingAsync(
        MaemoComplianceDbContext dbContext,
        Guid tenantId,
        IDateTimeProvider dateTimeProvider,
        IBusinessAuditLogger businessAuditLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            var now = dateTimeProvider.UtcNow.Date;
            var days90 = now.AddDays(90);
            var days30 = now.AddDays(30);
            var days7 = now.AddDays(7);

            // Find documents with retention dates within the next 90 days
            var upcomingRetention = await dbContext.Documents
                .Where(d => d.TenantId == tenantId
                    && d.RetainUntil.HasValue
                    && d.RetainUntil.Value.Date >= now
                    && d.RetainUntil.Value.Date <= days90)
                .ToListAsync(cancellationToken);

            if (upcomingRetention.Any())
            {
                _logger.LogInformation("Found {Count} document(s) with upcoming retention dates", upcomingRetention.Count);

                foreach (var document in upcomingRetention)
                {
                    var daysUntil = (document.RetainUntil!.Value.Date - now).Days;
                    var warningLevel = daysUntil <= 7 ? "7 days" : daysUntil <= 30 ? "30 days" : "90 days";

                    _logger.LogWarning(
                        "Document retention upcoming: {Title} (Id: {Id}) expires in {DaysUntil} days ({WarningLevel}) on {RetainUntil:yyyy-MM-dd} for tenant {TenantId}",
                        document.Title,
                        document.Id,
                        daysUntil,
                        warningLevel,
                        document.RetainUntil.Value,
                        tenantId);

                    // Log to business audit log
                    await businessAuditLogger.LogAsync(
                        "Document.RetentionUpcoming",
                        "Document",
                        document.Id.ToString(),
                        new
                        {
                            Title = document.Title,
                            RetainUntil = document.RetainUntil.Value.ToString("yyyy-MM-dd"),
                            DaysUntil = daysUntil,
                            WarningLevel = warningLevel,
                            Category = document.Category,
                            Department = document.Department
                        },
                        cancellationToken);
                }
            }
            else
            {
                _logger.LogDebug("No documents with upcoming retention dates found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing retention upcoming");
        }
    }

    private async Task ProcessRetentionExpiredAsync(
        MaemoComplianceDbContext dbContext,
        Guid tenantId,
        IDateTimeProvider dateTimeProvider,
        IBusinessAuditLogger businessAuditLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            var now = dateTimeProvider.UtcNow.Date;

            // Find documents with expired retention that are not locked
            var expiredRetention = await dbContext.Documents
                .Where(d => d.TenantId == tenantId
                    && d.RetainUntil.HasValue
                    && d.RetainUntil.Value.Date < now
                    && !d.IsRetentionLocked)
                .ToListAsync(cancellationToken);

            if (expiredRetention.Any())
            {
                _logger.LogWarning("Found {Count} document(s) with expired retention dates (not locked)", expiredRetention.Count);

                foreach (var document in expiredRetention)
                {
                    var daysExpired = (now - document.RetainUntil!.Value.Date).Days;

                    _logger.LogWarning(
                        "Document retention expired: {Title} (Id: {Id}) expired {DaysExpired} days ago on {RetainUntil:yyyy-MM-dd} for tenant {TenantId}. Ready for destruction workflow.",
                        document.Title,
                        document.Id,
                        daysExpired,
                        document.RetainUntil.Value,
                        tenantId);

                    // Log to business audit log
                    await businessAuditLogger.LogAsync(
                        "Document.RetentionExpired",
                        "Document",
                        document.Id.ToString(),
                        new
                        {
                            Title = document.Title,
                            RetainUntil = document.RetainUntil.Value.ToString("yyyy-MM-dd"),
                            DaysExpired = daysExpired,
                            Category = document.Category,
                            Department = document.Department,
                            Note = "Ready for destruction workflow - not locked"
                        },
                        cancellationToken);
                }
            }
            else
            {
                _logger.LogDebug("No documents with expired retention dates found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing retention expired");
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

