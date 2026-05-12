using Maemo.Application.Common;
using Maemo.Application.Documents.Dtos;
using Maemo.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Documents.Queries;

public class GetDocumentByIdQueryHandler : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IBusinessAuditLogger _businessAuditLogger;
    private readonly ICurrentUserService _currentUserService;

    public GetDocumentByIdQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IBusinessAuditLogger businessAuditLogger,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _businessAuditLogger = businessAuditLogger;
        _currentUserService = currentUserService;
    }

    public async Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var documentEntity = await _context.Documents
            .Where(d => d.Id == request.Id && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (documentEntity == null)
        {
            return null;
        }

        // Log document view for POPIA compliance if it contains personal information
        if (documentEntity.PersonalInformationType != PersonalInformationType.None)
        {
            await _businessAuditLogger.LogAsync(
                "Document.Viewed",
                "Document",
                documentEntity.Id.ToString(),
                new
                {
                    Title = documentEntity.Title,
                    PersonalInformationType = documentEntity.PersonalInformationType.ToString(),
                    PiiDataType = documentEntity.PiiDataType.ToString(),
                    ViewedBy = _currentUserService.UserId
                },
                cancellationToken);
        }

        var document = await _context.Documents
            .Where(d => d.Id == request.Id && d.TenantId == tenantId)
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
                IsRetentionLocked = d.IsRetentionLocked,
                IsPendingArchive = d.IsPendingArchive,
                Version = d.Version,
                ApproverUserId = d.ApproverUserId,
                ApprovedAt = d.ApprovedAt,
                Comments = d.Comments,
                IsCurrentVersion = d.IsCurrentVersion,
                PreviousVersionId = d.PreviousVersionId,
                StorageLocation = d.StorageLocation,
                FilePlanSeries = d.FilePlanSeries,
                FilePlanSubSeries = d.FilePlanSubSeries,
                FilePlanItem = d.FilePlanItem,
                LatestVersionNumber = d.Version,
                HasVersions = d.Versions.Any()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (document != null)
        {
            // Load latest version if exists
            var latestVersion = await _context.DocumentVersions
                .Where(dv => dv.DocumentId == request.Id && dv.IsLatest)
                .Select(dv => new DocumentVersionDto
                {
                    Id = dv.Id,
                    DocumentId = dv.DocumentId,
                    VersionNumber = dv.VersionNumber,
                    FileName = dv.FileName,
                    StorageLocation = dv.StorageLocation,
                    UploadedBy = dv.UploadedBy,
                    UploadedAt = dv.UploadedAt,
                    Comment = dv.Comment,
                    IsLatest = dv.IsLatest
                })
                .FirstOrDefaultAsync(cancellationToken);

            document.LatestVersion = latestVersion;
            if (latestVersion != null)
            {
                document.LatestVersionNumber = latestVersion.VersionNumber;
            }
        }

        return document;
    }
}

