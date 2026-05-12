using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Documents.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Queries;

/// <summary>
/// Handler for getting BBBEE certificates expiring soon.
/// </summary>
public class GetBbbeeCertificatesExpiringSoonQueryHandler : IRequestHandler<GetBbbeeCertificatesExpiringSoonQuery, IReadOnlyList<DocumentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetBbbeeCertificatesExpiringSoonQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<DocumentDto>> Handle(GetBbbeeCertificatesExpiringSoonQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var cutoffDate = _dateTimeProvider.UtcNow.AddDays(request.Days);

        var certificates = await _context.Documents
            .Where(d => 
                d.TenantId == tenantId &&
                d.Category == "BBBEE Certificate" &&
                d.BbbeeExpiryDate.HasValue &&
                d.BbbeeExpiryDate.Value <= cutoffDate &&
                d.BbbeeExpiryDate.Value >= _dateTimeProvider.UtcNow &&
                d.IsCurrentVersion)
            .OrderBy(d => d.BbbeeExpiryDate)
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

        return certificates;
    }
}

