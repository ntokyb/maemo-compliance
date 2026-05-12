using Maemo.Application.Common;
using Maemo.Application.Consultants.Dtos;
using Maemo.Domain.Documents;
using Maemo.Domain.Ncrs;
using Maemo.Domain.Risks;
using Maemo.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Consultants.Queries;

public class GetConsultantDashboardSummaryQueryHandler : IRequestHandler<GetConsultantDashboardSummaryQuery, ConsultantDashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetConsultantDashboardSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<ConsultantDashboardSummaryDto> Handle(GetConsultantDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(currentUserId))
        {
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var userId = Guid.Parse(currentUserId);

        // Verify user is a consultant
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || user.Role != UserRole.Consultant)
        {
            throw new UnauthorizedAccessException("User is not a consultant.");
        }

        // Get all active tenant IDs linked to this consultant
        var tenantIds = await _context.ConsultantTenantLinks
            .Where(l => l.ConsultantUserId == userId && l.IsActive)
            .Select(l => l.TenantId)
            .ToListAsync(cancellationToken);

        if (tenantIds.Count == 0)
        {
            return new ConsultantDashboardSummaryDto
            {
                TotalClients = 0,
                TotalOpenNcrs = 0,
                TotalHighSeverityNcrs = 0,
                TotalHighRisks = 0,
                UpcomingDocumentReviews = 0
            };
        }

        var today = _dateTimeProvider.UtcNow.Date;
        var reviewDateThreshold = today.AddDays(14);

        // Aggregate metrics across all linked tenants
        // Note: We need to bypass tenant query filters since we're aggregating across multiple tenants
        // EF Core query filters automatically filter by current tenant, so we use IgnoreQueryFilters()
        // and then filter manually by TenantId in the list
        
        // Query NCRs across all linked tenants (bypass tenant filter)
        var totalOpenNcrs = await _context.Ncrs
            .IgnoreQueryFilters()
            .Where(n => tenantIds.Contains(n.TenantId) && n.Status != NcrStatus.Closed)
            .CountAsync(cancellationToken);

        var totalHighSeverityNcrs = await _context.Ncrs
            .IgnoreQueryFilters()
            .Where(n => tenantIds.Contains(n.TenantId) 
                && n.Status != NcrStatus.Closed 
                && n.Severity == NcrSeverity.High)
            .CountAsync(cancellationToken);

        // Query Risks across all linked tenants (bypass tenant filter)
        var totalHighRisks = await _context.Risks
            .IgnoreQueryFilters()
            .Where(r => tenantIds.Contains(r.TenantId) && r.ResidualScore >= 15)
            .CountAsync(cancellationToken);

        // Query Documents across all linked tenants (upcoming reviews)
        var upcomingDocumentReviews = await _context.Documents
            .IgnoreQueryFilters()
            .Where(d => tenantIds.Contains(d.TenantId) 
                && d.IsCurrentVersion
                && d.ReviewDate.Date >= today 
                && d.ReviewDate.Date <= reviewDateThreshold)
            .CountAsync(cancellationToken);

        return new ConsultantDashboardSummaryDto
        {
            TotalClients = tenantIds.Count,
            TotalOpenNcrs = totalOpenNcrs,
            TotalHighSeverityNcrs = totalHighSeverityNcrs,
            TotalHighRisks = totalHighRisks,
            UpcomingDocumentReviews = upcomingDocumentReviews
        };
    }
}

