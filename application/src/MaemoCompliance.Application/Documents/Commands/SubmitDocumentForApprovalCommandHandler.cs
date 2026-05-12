using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Commands;

public class SubmitDocumentForApprovalCommandHandler : IRequestHandler<SubmitDocumentForApprovalCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public SubmitDocumentForApprovalCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IAuditLogger auditLogger,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _auditLogger = auditLogger;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(SubmitDocumentForApprovalCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var document = await _context.Documents
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {request.DocumentId} not found.");
        }

        // Validate state transition using state machine
        if (!DocumentWorkflowStateMachine.CanTransition(document.WorkflowState, DocumentWorkflowState.PendingApproval))
        {
            throw new InvalidOperationException(
                $"Document cannot transition from {document.WorkflowState} to PendingApproval. Allowed transitions: {string.Join(", ", DocumentWorkflowStateMachine.GetAllowedTransitions(document.WorkflowState))}.");
        }

        // Ensure document has a file uploaded
        if (string.IsNullOrWhiteSpace(document.StorageLocation) && !document.Versions.Any())
        {
            throw new InvalidOperationException(
                "Document must have at least one file version uploaded before it can be submitted for approval.");
        }

        // Transition to PendingApproval
        var previousState = document.WorkflowState;
        document.WorkflowState = DocumentWorkflowState.PendingApproval;
        document.Status = DocumentWorkflowStateMachine.MapToDocumentStatus(document.WorkflowState);
        document.SubmittedForReviewAt = _dateTimeProvider.UtcNow;
        document.ModifiedAt = _dateTimeProvider.UtcNow;
        document.ModifiedBy = _currentUserService.UserId;
        document.RejectedReason = null; // Clear any previous rejection reason

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "SubmitDocumentForApproval",
            "Document",
            document.Id,
            new
            {
                PreviousState = previousState.ToString(),
                NewState = document.WorkflowState.ToString(),
                SubmittedBy = _currentUserService.UserId
            },
            cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Document.SubmittedForApproval",
            "Document",
            document.Id.ToString(),
            new
            {
                PreviousState = previousState.ToString(),
                NewState = document.WorkflowState.ToString(),
                SubmittedBy = _currentUserService.UserId,
                Title = document.Title
            },
            cancellationToken);
    }
}

