using MediatR;

namespace MaemoCompliance.Application.Admin.Dashboard;

/// <summary>
/// Query to get admin dashboard summary - platform-wide metrics.
/// </summary>
public sealed record GetAdminDashboardSummaryQuery() : IRequest<AdminDashboardSummaryDto>;

