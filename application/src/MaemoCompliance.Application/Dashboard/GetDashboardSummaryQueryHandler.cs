using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Dashboard;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.Domain.Risks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Dashboard;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetDashboardSummaryQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var today = _dateTimeProvider.UtcNow.Date;

        // Documents queries - only count current versions
        var totalDocuments = await _context.Documents
            .Where(d => d.TenantId == tenantId && d.IsCurrentVersion)
            .CountAsync(cancellationToken);

        var activeDocuments = await _context.Documents
            .Where(d => d.TenantId == tenantId && d.IsCurrentVersion && d.Status == DocumentStatus.Active)
            .CountAsync(cancellationToken);

        // NCRs queries
        var totalNcrs = await _context.Ncrs
            .Where(n => n.TenantId == tenantId)
            .CountAsync(cancellationToken);

        var openNcrs = await _context.Ncrs
            .Where(n => n.TenantId == tenantId && n.Status != NcrStatus.Closed)
            .CountAsync(cancellationToken);

        var overdueNcrs = await _context.Ncrs
            .Where(n => n.TenantId == tenantId 
                && n.Status != NcrStatus.Closed 
                && n.DueDate.HasValue 
                && n.DueDate.Value.Date < today)
            .CountAsync(cancellationToken);

        // Risk queries
        var totalRisks = await _context.Risks
            .Where(r => r.TenantId == tenantId)
            .CountAsync(cancellationToken);

        var highRisks = await _context.Risks
            .Where(r => r.TenantId == tenantId && r.ResidualScore >= 15)
            .CountAsync(cancellationToken);

        var mediumRisks = await _context.Risks
            .Where(r => r.TenantId == tenantId && r.ResidualScore >= 8 && r.ResidualScore < 15)
            .CountAsync(cancellationToken);

        var lowRisks = await _context.Risks
            .Where(r => r.TenantId == tenantId && r.ResidualScore < 8)
            .CountAsync(cancellationToken);

        return new DashboardSummaryDto
        {
            TotalDocuments = totalDocuments,
            ActiveDocuments = activeDocuments,
            TotalNcrs = totalNcrs,
            OpenNcrs = openNcrs,
            OverdueNcrs = overdueNcrs,
            TotalRisks = totalRisks,
            HighRisks = highRisks,
            MediumRisks = mediumRisks,
            LowRisks = lowRisks
        };
    }
}

