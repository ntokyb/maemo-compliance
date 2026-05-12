using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetPopiaDocumentSummaryQueryHandler : IRequestHandler<GetPopiaDocumentSummaryQuery, PopiaDocumentSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetPopiaDocumentSummaryQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<PopiaDocumentSummaryDto> Handle(GetPopiaDocumentSummaryQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var documents = await _context.Documents
            .Where(d => d.TenantId == tenantId && d.IsCurrentVersion)
            .ToListAsync(cancellationToken);

        var summary = new PopiaDocumentSummaryDto
        {
            TotalDocuments = documents.Count,
            DocumentsWithNoPersonalInfo = documents.Count(d => d.PersonalInformationType == PersonalInformationType.None),
            DocumentsWithPersonalInfo = documents.Count(d => d.PersonalInformationType == PersonalInformationType.PersonalInfo),
            DocumentsWithSpecialPersonalInfo = documents.Count(d => d.PersonalInformationType == PersonalInformationType.SpecialPersonalInfo)
        };

        // Group by category
        var byCategory = documents
            .GroupBy(d => d.Category ?? "Uncategorized")
            .Select(g => new PopiaDocumentSummaryByCategoryDto
            {
                Category = g.Key,
                Total = g.Count(),
                WithNoPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.None),
                WithPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.PersonalInfo),
                WithSpecialPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.SpecialPersonalInfo)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        summary.ByCategory = byCategory;

        // Group by department
        var byDepartment = documents
            .Where(d => !string.IsNullOrWhiteSpace(d.Department))
            .GroupBy(d => d.Department!)
            .Select(g => new PopiaDocumentSummaryByDepartmentDto
            {
                Department = g.Key,
                Total = g.Count(),
                WithNoPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.None),
                WithPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.PersonalInfo),
                WithSpecialPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.SpecialPersonalInfo)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        summary.ByDepartment = byDepartment;

        // Group by owner
        var byOwner = documents
            .Where(d => !string.IsNullOrWhiteSpace(d.OwnerUserId))
            .GroupBy(d => d.OwnerUserId!)
            .Select(g => new PopiaDocumentSummaryByOwnerDto
            {
                OwnerUserId = g.Key,
                Total = g.Count(),
                WithNoPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.None),
                WithPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.PersonalInfo),
                WithSpecialPersonalInfo = g.Count(d => d.PersonalInformationType == PersonalInformationType.SpecialPersonalInfo)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        summary.ByOwner = byOwner;

        return summary;
    }
}

