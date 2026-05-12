using MaemoCompliance.Application.Common;
using MaemoCompliance.Application.Documents.Dtos;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Commands;

public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IRetentionPolicyService _retentionPolicyService;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public CreateDocumentCommandHandler(
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

    public async Task<Guid> Handle(CreateDocumentCommand request, CancellationToken cancellationToken)
    {
        // Calculate retention date if not provided
        var retainUntil = request.Request.RetainUntil ?? 
            _retentionPolicyService.CalculateRetainUntil(request.Request.Category, request.Request.Department);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantProvider.GetCurrentTenantId(),
            Title = request.Request.Title,
            Category = request.Request.Category,
            Department = request.Request.Department,
            OwnerUserId = request.Request.OwnerUserId,
            ReviewDate = request.Request.ReviewDate,
            Status = DocumentStatus.Draft,
            WorkflowState = DocumentWorkflowState.Draft,
            PiiDataType = request.Request.PiiDataType,
            PersonalInformationType = request.Request.PersonalInformationType,
            PiiType = request.Request.PiiType,
            PiiDescription = request.Request.PiiDescription,
            PiiRetentionPeriodInMonths = request.Request.PiiRetentionPeriodInMonths,
            BbbeeExpiryDate = request.Request.BbbeeExpiryDate,
            BbbeeLevel = request.Request.BbbeeLevel,
            RetainUntil = retainUntil,
            IsRetentionLocked = request.Request.IsRetentionLocked,
            FilePlanSeries = request.Request.FilePlanSeries,
            FilePlanSubSeries = request.Request.FilePlanSubSeries,
            FilePlanItem = request.Request.FilePlanItem,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.UserId
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "CreateDocument",
            "Document",
            document.Id,
            new { Title = document.Title, Category = document.Category, Status = document.Status },
            cancellationToken);

        // Business audit log for retention
        if (retainUntil.HasValue)
        {
            await _businessAuditLogger.LogAsync(
                "Document.RetentionSet",
                "Document",
                document.Id.ToString(),
                new 
                { 
                    RetainUntil = retainUntil.Value.ToString("yyyy-MM-dd"),
                    Category = document.Category,
                    Department = document.Department,
                    IsRetentionLocked = document.IsRetentionLocked
                },
                cancellationToken);
        }

        // Business audit log for PII if set
        if (document.PiiType != PiiType.None)
        {
            await _businessAuditLogger.LogAsync(
                "Document.PiiChanged",
                "Document",
                document.Id.ToString(),
                new 
                { 
                    PiiType = document.PiiType.ToString(),
                    PiiDescription = document.PiiDescription,
                    PiiRetentionPeriodInMonths = document.PiiRetentionPeriodInMonths,
                    Title = document.Title
                },
                cancellationToken);
        }

        return document.Id;
    }
}

