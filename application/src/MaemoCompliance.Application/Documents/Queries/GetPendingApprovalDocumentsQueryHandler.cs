using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetPendingApprovalDocumentsQueryHandler : IRequestHandler<GetPendingApprovalDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetPendingApprovalDocumentsQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<DocumentDto>> Handle(GetPendingApprovalDocumentsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var documents = await _context.Documents
            .Where(d => d.TenantId == tenantId && d.WorkflowState == DocumentWorkflowState.PendingApproval)
            .OrderByDescending(d => d.CreatedAt)
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
                PreviousVersionId = d.PreviousVersionId,
                StorageLocation = d.StorageLocation,
                LatestVersionNumber = d.Version,
                HasVersions = d.Versions.Any()
            })
            .ToListAsync(cancellationToken);

        return documents;
    }
}

