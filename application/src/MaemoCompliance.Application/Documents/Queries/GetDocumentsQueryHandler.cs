using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetDocumentsQueryHandler : IRequestHandler<GetDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetDocumentsQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<DocumentDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var query = _context.Documents
            .Where(d => d.TenantId == tenantId);

        // Filter by current version by default unless IncludeAllVersions is true
        if (!request.IncludeAllVersions)
        {
            query = query.Where(d => d.IsCurrentVersion);
        }

        // Apply filters
        if (request.Status.HasValue)
        {
            query = query.Where(d => d.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(d => d.Department == request.Department);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(d => d.Category == request.Category);
        }

        var documents = await query
            .OrderBy(d => d.Title)
            .ThenByDescending(d => d.Version)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                Title = d.Title,
                Category = d.Category,
                Department = d.Department,
                OwnerUserId = d.OwnerUserId,
                ReviewDate = d.ReviewDate,
                Status = d.Status,
                WorkflowState = d.WorkflowState,
                RejectedReason = d.RejectedReason,
                PiiDataType = d.PiiDataType,
                PersonalInformationType = d.PersonalInformationType,
                PiiType = d.PiiType,
                PiiDescription = d.PiiDescription,
                PiiRetentionPeriodInMonths = d.PiiRetentionPeriodInMonths,
                BbbeeExpiryDate = d.BbbeeExpiryDate,
                BbbeeLevel = d.BbbeeLevel,
                RetainUntil = d.RetainUntil,
                FilePlanSeries = d.FilePlanSeries,
                FilePlanSubSeries = d.FilePlanSubSeries,
                FilePlanItem = d.FilePlanItem,
                IsRetentionLocked = d.IsRetentionLocked,
                IsPendingArchive = d.IsPendingArchive,
                Version = d.Version,
                ApproverUserId = d.ApproverUserId,
                ApprovedAt = d.ApprovedAt,
                Comments = d.Comments,
                IsCurrentVersion = d.IsCurrentVersion,
                PreviousVersionId = d.PreviousVersionId
            })
            .ToListAsync(cancellationToken);

        return documents;
    }
}

