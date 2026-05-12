using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Commands;

public class UpdateDocumentCommandHandler : IRequestHandler<UpdateDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IRetentionPolicyService _retentionPolicyService;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UpdateDocumentCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IAuditLogger auditLogger,
        IRetentionPolicyService retentionPolicyService,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _auditLogger = auditLogger;
        _retentionPolicyService = retentionPolicyService;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(UpdateDocumentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.Id && d.TenantId == tenantId, cancellationToken);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {request.Id} not found.");
        }

        // Enforce editing restrictions: Only Draft documents can be edited
        if (!DocumentWorkflowStateMachine.CanEdit(document.WorkflowState))
        {
            throw new InvalidOperationException(
                $"Document cannot be edited in {document.WorkflowState} state. Only Draft documents can be edited.");
        }

        // Track changes for retention recalculation
        var categoryChanged = document.Category != request.Request.Category;
        var departmentChanged = document.Department != request.Request.Department;
        var oldRetainUntil = document.RetainUntil;
        var oldIsRetentionLocked = document.IsRetentionLocked;
        
        // Track PII changes for audit logging
        var oldPiiType = document.PiiType;
        var oldPiiDescription = document.PiiDescription;
        var oldPiiRetentionPeriod = document.PiiRetentionPeriodInMonths;

        document.Title = request.Request.Title;
        document.Category = request.Request.Category;
        document.Department = request.Request.Department;
        document.OwnerUserId = request.Request.OwnerUserId;
        document.ReviewDate = request.Request.ReviewDate;
        document.Status = request.Request.Status;
        document.PiiDataType = request.Request.PiiDataType;
        document.PersonalInformationType = request.Request.PersonalInformationType;
        document.PiiType = request.Request.PiiType;
        document.PiiDescription = request.Request.PiiDescription;
        document.PiiRetentionPeriodInMonths = request.Request.PiiRetentionPeriodInMonths;
        document.BbbeeExpiryDate = request.Request.BbbeeExpiryDate;
        document.BbbeeLevel = request.Request.BbbeeLevel;
        document.IsRetentionLocked = request.Request.IsRetentionLocked;
        document.FilePlanSeries = request.Request.FilePlanSeries;
        document.FilePlanSubSeries = request.Request.FilePlanSubSeries;
        document.FilePlanItem = request.Request.FilePlanItem;
        document.ModifiedAt = _dateTimeProvider.UtcNow;
        document.ModifiedBy = _currentUserService.UserId;

        // Recalculate retention if category/department changed and retention is not locked
        // Only recalculate if RetainUntil was not explicitly provided in request
        if (!document.IsRetentionLocked && (categoryChanged || departmentChanged) && !request.Request.RetainUntil.HasValue)
        {
            document.RetainUntil = _retentionPolicyService.CalculateRetainUntil(document.Category, document.Department);
        }
        else if (request.Request.RetainUntil.HasValue)
        {
            // Explicit retention date provided
            document.RetainUntil = request.Request.RetainUntil;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log for retention changes
        if (oldRetainUntil != document.RetainUntil)
        {
            await _businessAuditLogger.LogAsync(
                oldRetainUntil == null ? "Document.RetentionSet" : "Document.RetentionChanged",
                "Document",
                document.Id.ToString(),
                new 
                { 
                    OldRetainUntil = oldRetainUntil?.ToString("yyyy-MM-dd"),
                    NewRetainUntil = document.RetainUntil?.ToString("yyyy-MM-dd"),
                    Category = document.Category,
                    Department = document.Department
                },
                cancellationToken);
        }

        if (oldIsRetentionLocked != document.IsRetentionLocked)
        {
            await _businessAuditLogger.LogAsync(
                "Document.RetentionLockChanged",
                "Document",
                document.Id.ToString(),
                new 
                { 
                    IsRetentionLocked = document.IsRetentionLocked,
                    RetainUntil = document.RetainUntil?.ToString("yyyy-MM-dd")
                },
                cancellationToken);
        }

        // Business audit log for PII changes
        if (oldPiiType != document.PiiType || 
            oldPiiDescription != document.PiiDescription || 
            oldPiiRetentionPeriod != document.PiiRetentionPeriodInMonths)
        {
            await _businessAuditLogger.LogAsync(
                "Document.PiiChanged",
                "Document",
                document.Id.ToString(),
                new 
                { 
                    OldPiiType = oldPiiType.ToString(),
                    NewPiiType = document.PiiType.ToString(),
                    OldPiiDescription = oldPiiDescription,
                    NewPiiDescription = document.PiiDescription,
                    OldPiiRetentionPeriodInMonths = oldPiiRetentionPeriod,
                    NewPiiRetentionPeriodInMonths = document.PiiRetentionPeriodInMonths,
                    Title = document.Title
                },
                cancellationToken);
        }

        // Audit log - include BBBEE fields if category is BBBEE Certificate
        var auditMetadata = new Dictionary<string, object?>
        {
            { "Title", document.Title },
            { "Category", document.Category },
            { "Status", document.Status.ToString() }
        };

        if (document.Category == "BBBEE Certificate")
        {
            auditMetadata["BbbeeExpiryDate"] = document.BbbeeExpiryDate?.ToString("yyyy-MM-dd");
            auditMetadata["BbbeeLevel"] = document.BbbeeLevel;
        }

        await _auditLogger.LogAsync(
            "UpdateDocument",
            "Document",
            document.Id,
            auditMetadata,
            cancellationToken);
    }
}

