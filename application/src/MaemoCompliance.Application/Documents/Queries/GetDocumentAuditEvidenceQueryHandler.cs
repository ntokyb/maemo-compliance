using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.AuditLog;
using MaemoCompliance.Domain.Documents;
using MaemoCompliance.Domain.Ncrs;
using MaemoCompliance.Domain.Risks;
using MaemoCompliance.Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetDocumentAuditEvidenceQueryHandler : IRequestHandler<GetDocumentAuditEvidenceQuery, AuditEvidenceDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMediator _mediator;
    private readonly IBusinessAuditLogger _businessAuditLogger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetDocumentAuditEvidenceQueryHandler> _logger;

    public GetDocumentAuditEvidenceQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        IMediator mediator,
        IBusinessAuditLogger businessAuditLogger,
        ICurrentUserService currentUserService,
        ILogger<GetDocumentAuditEvidenceQueryHandler> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _mediator = mediator;
        _businessAuditLogger = businessAuditLogger;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AuditEvidenceDto> Handle(GetDocumentAuditEvidenceQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var documentIdString = request.DocumentId.ToString();
        var currentUserId = _currentUserService.UserId;

        // Get document - throw KeyNotFoundException if not found (consistent with other handlers)
        var document = await _context.Documents
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new KeyNotFoundException($"Document with ID {request.DocumentId} not found for current tenant.");
        }

        // Get versions - ensure empty list if none exist
        var versionsQuery = new GetDocumentVersionsQuery { DocumentId = request.DocumentId };
        var versions = await _mediator.Send(versionsQuery, cancellationToken);
        var versionsList = versions?.ToList() ?? new List<DocumentVersionDto>();

        // Get business audit logs for this document - ensure empty list if none exist
        var businessAuditLogs = await _context.BusinessAuditLogs
            .Where(log => log.EntityType == "Document" && log.EntityId == documentIdString)
            .OrderBy(log => log.Timestamp)
            .ToListAsync(cancellationToken);

        // Safely parse metadata JSON with error handling
        var businessAuditLogEntries = new List<BusinessAuditLogEntryDto>();
        foreach (var log in businessAuditLogs)
        {
            string? safeMetadataJson = null;
            if (!string.IsNullOrWhiteSpace(log.MetadataJson))
            {
                try
                {
                    // Validate JSON by attempting to parse it
                    JsonSerializer.Deserialize<Dictionary<string, object>>(log.MetadataJson);
                    safeMetadataJson = log.MetadataJson;
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(
                        "Failed to parse metadata JSON for business audit log {LogId}: {Error}",
                        log.Id,
                        ex.Message);
                    // Set to null - will be omitted from response
                    safeMetadataJson = null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Unexpected error parsing metadata JSON for business audit log {LogId}: {Error}",
                        log.Id,
                        ex.Message);
                    safeMetadataJson = null;
                }
            }

            businessAuditLogEntries.Add(new BusinessAuditLogEntryDto
            {
                Id = log.Id,
                Action = log.Action,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Timestamp = log.Timestamp,
                Username = log.Username,
                MetadataJson = safeMetadataJson
            });
        }

        // Find linked NCRs - ensure empty list if none exist
        var linkedNcrs = new List<LinkedNcrDto>();
        var documentTitle = document.Title;
        
        if (!string.IsNullOrWhiteSpace(documentTitle))
        {
            // Search NCRs that might reference this document
            // Load all NCRs for the tenant and filter in memory (for now)
            // TODO: Consider adding explicit Document-NCR links table for better performance
            var allNcrs = await _context.Ncrs
                .Where(n => n.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            
            var matchingNcrs = allNcrs
                .Where(n => (!string.IsNullOrWhiteSpace(n.Description) && n.Description.Contains(documentTitle, StringComparison.OrdinalIgnoreCase)) || 
                           (!string.IsNullOrWhiteSpace(n.Title) && n.Title.Contains(documentTitle, StringComparison.OrdinalIgnoreCase)))
                .Select(n => new LinkedNcrDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Description = n.Description,
                    Department = n.Department,
                    LinkReason = "Referenced in NCR description or title"
                })
                .ToList();
            
            linkedNcrs.AddRange(matchingNcrs);
        }

        // Find linked Risks - ensure empty list if none exist
        var linkedRisks = new List<LinkedRiskDto>();
        
        if (!string.IsNullOrWhiteSpace(documentTitle))
        {
            var allRisks = await _context.Risks
                .Where(r => r.TenantId == tenantId)
                .ToListAsync(cancellationToken);
            
            var matchingRisks = allRisks
                .Where(r => (!string.IsNullOrWhiteSpace(r.Description) && r.Description.Contains(documentTitle, StringComparison.OrdinalIgnoreCase)) || 
                           (!string.IsNullOrWhiteSpace(r.Title) && r.Title.Contains(documentTitle, StringComparison.OrdinalIgnoreCase)))
                .Select(r => new LinkedRiskDto
                {
                    Id = r.Id,
                    Title = r.Title,
                    Description = r.Description,
                    LinkReason = "Referenced in Risk description or title"
                })
                .ToList();
            
            linkedRisks.AddRange(matchingRisks);
        }

        // Build approval history - ensure non-null
        var approvalHistory = new ApprovalHistoryDto
        {
            ApproverUserId = document.ApproverUserId,
            ApprovedAt = document.ApprovedAt,
            Comments = document.Comments,
            CurrentWorkflowState = document.WorkflowState,
            RejectedReason = document.RejectedReason
        };

        var generatedAt = _dateTimeProvider.UtcNow;
        var generatedBy = currentUserId ?? "System";

        // Log business audit event for evidence generation
        try
        {
            await _businessAuditLogger.LogAsync(
                "Document.EvidenceGenerated",
                "Document",
                document.Id.ToString(),
                new
                {
                    GeneratedAt = generatedAt,
                    GeneratedBy = generatedBy,
                    DocumentTitle = document.Title
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error but don't fail the request
            _logger.LogError(
                ex,
                "Failed to log business audit event for evidence generation for document {DocumentId}",
                document.Id);
        }

        return new AuditEvidenceDto
        {
            Document = document,
            Versions = versionsList,
            BusinessAuditLogs = businessAuditLogEntries,
            LinkedNcrs = linkedNcrs,
            LinkedRisks = linkedRisks,
            ApprovalHistory = approvalHistory,
            GeneratedAt = generatedAt
        };
    }
}

