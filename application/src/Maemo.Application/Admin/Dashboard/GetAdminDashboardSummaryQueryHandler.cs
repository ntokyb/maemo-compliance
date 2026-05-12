using Maemo.Application.Common;
using Maemo.Domain.Logging;
using Maemo.Domain.Tenants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Admin.Dashboard;

/// <summary>
/// Handler for admin dashboard summary query - platform-wide metrics.
/// </summary>
public class GetAdminDashboardSummaryQueryHandler : IRequestHandler<GetAdminDashboardSummaryQuery, AdminDashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAdminDashboardSummaryQueryHandler(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AdminDashboardSummaryDto> Handle(GetAdminDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;
        var yesterday = now.AddDays(-1);

        // Count tenants by status
        // Note: Tenant status is currently IsActive boolean + TrialEndsAt
        // TODO: Consider adding TenantStatus enum (Active, Suspended, Trial, Expired) for better status management
        var totalTenants = await _context.Tenants
            .CountAsync(cancellationToken);

        var activeTenants = await _context.Tenants
            .Where(t => t.IsActive)
            .CountAsync(cancellationToken);

        var suspendedTenants = await _context.Tenants
            .Where(t => !t.IsActive)
            .CountAsync(cancellationToken);

        var trialTenants = await _context.Tenants
            .Where(t => t.TrialEndsAt.HasValue && t.TrialEndsAt.Value > now)
            .CountAsync(cancellationToken);

        // Query logs from last 24 hours
        var totalApiCallsLast24h = await _context.ApiCallLogs
            .Where(log => log.Timestamp >= yesterday)
            .CountAsync(cancellationToken);

        var totalErrorsLast24h = await _context.ErrorLogs
            .Where(log => log.Timestamp >= yesterday)
            .CountAsync(cancellationToken);

        var webhookFailuresLast24h = await _context.WebhookDeliveryLogs
            .Where(log => log.Timestamp >= yesterday && !log.Success)
            .CountAsync(cancellationToken);

        var workerFailuresLast24h = await _context.WorkerJobLogs
            .Where(log => log.Timestamp >= yesterday && log.Status == "Failed")
            .CountAsync(cancellationToken);

        return new AdminDashboardSummaryDto(
            TotalTenants: totalTenants,
            ActiveTenants: activeTenants,
            SuspendedTenants: suspendedTenants,
            TrialTenants: trialTenants,
            TotalApiCallsLast24h: totalApiCallsLast24h,
            TotalErrorsLast24h: totalErrorsLast24h,
            WebhookFailuresLast24h: webhookFailuresLast24h,
            WorkerFailuresLast24h: workerFailuresLast24h
        );
    }
}

