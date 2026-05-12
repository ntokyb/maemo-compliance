using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Evidence.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Evidence.Queries;

public class GetEvidenceRegisterQueryHandler : IRequestHandler<GetEvidenceRegisterQuery, IReadOnlyList<EvidenceItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetEvidenceRegisterQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<EvidenceItemDto>> Handle(GetEvidenceRegisterQuery request, CancellationToken cancellationToken)
    {
        var results = new List<EvidenceItemDto>();

        // Get current tenant ID (for filtering if not specified)
        var currentTenantId = _tenantProvider.GetCurrentTenantId();
        var filterTenantId = request.TenantId ?? currentTenantId;

        // Query DocumentVersions
        if (string.IsNullOrEmpty(request.EntityType) || request.EntityType == "DocumentVersion")
        {
            var documentVersionsQuery = _context.DocumentVersions
                .Include(dv => dv.Document)
                .Where(dv => dv.Document.TenantId == filterTenantId);

            if (request.FromDate.HasValue)
            {
                documentVersionsQuery = documentVersionsQuery.Where(dv => dv.UploadedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                documentVersionsQuery = documentVersionsQuery.Where(dv => dv.UploadedAt <= request.ToDate.Value);
            }

            var documentVersions = await documentVersionsQuery
                .OrderByDescending(dv => dv.UploadedAt)
                .Take(request.Limit)
                .Select(dv => new EvidenceItemDto
                {
                    Id = dv.Id,
                    EntityType = "DocumentVersion",
                    EntityId = dv.DocumentId.ToString(),
                    FileName = dv.FileName,
                    StorageLocation = dv.StorageLocation,
                    FileHash = dv.FileHash,
                    UploadedAt = dv.UploadedAt,
                    UploadedBy = dv.UploadedBy,
                    TenantId = dv.Document.TenantId,
                    DocumentTitle = dv.Document.Title,
                    VersionNumber = dv.VersionNumber
                })
                .ToListAsync(cancellationToken);

            results.AddRange(documentVersions);
        }

        // Query Documents (main file, not versions)
        if (string.IsNullOrEmpty(request.EntityType) || request.EntityType == "Document")
        {
            var documentsQuery = _context.Documents
                .Where(d => d.TenantId == filterTenantId && !string.IsNullOrEmpty(d.StorageLocation));

            // Only include documents that don't have versions (to avoid duplicates)
            documentsQuery = documentsQuery.Where(d => !d.Versions.Any());

            if (request.FromDate.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                documentsQuery = documentsQuery.Where(d => d.CreatedAt <= request.ToDate.Value);
            }

            var documents = await documentsQuery
                .OrderByDescending(d => d.CreatedAt)
                .Take(request.Limit)
                .Select(d => new EvidenceItemDto
                {
                    Id = d.Id,
                    EntityType = "Document",
                    EntityId = d.Id.ToString(),
                    FileName = d.Title + ".pdf", // Default filename
                    StorageLocation = d.StorageLocation!,
                    FileHash = d.FileHash,
                    UploadedAt = d.CreatedAt,
                    UploadedBy = d.CreatedBy,
                    TenantId = d.TenantId,
                    DocumentTitle = d.Title
                })
                .ToListAsync(cancellationToken);

            results.AddRange(documents);
        }

        // Query AuditAnswers (evidence files)
        if (string.IsNullOrEmpty(request.EntityType) || request.EntityType == "AuditAnswer")
        {
            var auditAnswersQuery = _context.AuditAnswers
                .Where(a => a.TenantId == filterTenantId && !string.IsNullOrEmpty(a.EvidenceFileUrl));

            if (request.FromDate.HasValue)
            {
                auditAnswersQuery = auditAnswersQuery.Where(a => a.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                auditAnswersQuery = auditAnswersQuery.Where(a => a.CreatedAt <= request.ToDate.Value);
            }

            var auditAnswers = await auditAnswersQuery
                .OrderByDescending(a => a.CreatedAt)
                .Take(request.Limit)
                .ToListAsync(cancellationToken);

            var auditAnswerDtos = auditAnswers.Select(a =>
            {
                var fileName = "evidence";
                if (!string.IsNullOrEmpty(a.EvidenceFileUrl))
                {
                    var parts = a.EvidenceFileUrl.Split('/');
                    if (parts.Length > 0)
                    {
                        fileName = parts[parts.Length - 1];
                    }
                }

                return new EvidenceItemDto
                {
                    Id = a.Id,
                    EntityType = "AuditAnswer",
                    EntityId = a.AuditRunId.ToString(),
                    FileName = fileName,
                    StorageLocation = a.EvidenceFileUrl ?? string.Empty,
                    FileHash = a.EvidenceFileHash,
                    UploadedAt = a.CreatedAt,
                    UploadedBy = a.CreatedBy,
                    TenantId = a.TenantId,
                    AuditRunId = a.AuditRunId,
                    AuditQuestionId = a.AuditQuestionId
                };
            }).ToList();

            results.AddRange(auditAnswerDtos);
        }

        // Sort all results by UploadedAt descending and limit
        return results
            .OrderByDescending(r => r.UploadedAt)
            .Take(request.Limit)
            .ToList();
    }
}

