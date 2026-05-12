namespace MaemoCompliance.Application.Admin.Dashboard;

/// <summary>
/// Admin dashboard summary DTO - Platform-wide metrics for Codist staff.
/// </summary>
public sealed record AdminDashboardSummaryDto(
    int TotalTenants,
    int ActiveTenants,
    int SuspendedTenants,
    int TrialTenants,
    int TotalApiCallsLast24h,
    int TotalErrorsLast24h,
    int WebhookFailuresLast24h,
    int WorkerFailuresLast24h
);

