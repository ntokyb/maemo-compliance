using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Commands;

public class RejectDocumentCommandHandler : IRequestHandler<RejectDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public RejectDocumentCommandHandler(
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

    public async Task Handle(RejectDocumentCommand request, CancellationToken cancellationToken)
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
        if (!DocumentWorkflowStateMachine.CanTransition(document.WorkflowState, DocumentWorkflowState.Draft))
        {
            throw new InvalidOperationException(
                $"Document cannot transition from {document.WorkflowState} to Draft (rejected). Allowed transitions: {string.Join(", ", DocumentWorkflowStateMachine.GetAllowedTransitions(document.WorkflowState))}.");
        }

        // Transition back to Draft with rejection reason
        var previousState = document.WorkflowState;
        document.WorkflowState = DocumentWorkflowState.Draft;
        document.Status = DocumentWorkflowStateMachine.MapToDocumentStatus(document.WorkflowState);
        document.RejectedReason = request.RejectedReason;
        document.ApproverUserId = null; // Clear approver
        document.ApprovedAt = null; // Clear approval date
        document.ModifiedAt = _dateTimeProvider.UtcNow;
        document.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "RejectDocument",
            "Document",
            document.Id,
            new
            {
                PreviousState = previousState.ToString(),
                NewState = document.WorkflowState.ToString(),
                RejectedBy = _currentUserService.UserId,
                RejectedReason = request.RejectedReason
            },
            cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Document.Rejected",
            "Document",
            document.Id.ToString(),
            new
            {
                PreviousState = previousState.ToString(),
                NewState = document.WorkflowState.ToString(),
                RejectedBy = _currentUserService.UserId,
                RejectedReason = request.RejectedReason,
                Title = document.Title
            },
            cancellationToken);
    }
}

