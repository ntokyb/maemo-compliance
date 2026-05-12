using Maemo.Application.Common;
using Maemo.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Documents.Commands;

public class ApproveDocumentCommandHandler : IRequestHandler<ApproveDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public ApproveDocumentCommandHandler(
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

    public async Task Handle(ApproveDocumentCommand request, CancellationToken cancellationToken)
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
        if (!DocumentWorkflowStateMachine.CanTransition(document.WorkflowState, DocumentWorkflowState.Approved))
        {
            throw new InvalidOperationException(
                $"Document cannot transition from {document.WorkflowState} to Approved. Allowed transitions: {string.Join(", ", DocumentWorkflowStateMachine.GetAllowedTransitions(document.WorkflowState))}.");
        }

        // Transition to Approved
        var previousState = document.WorkflowState;
        document.WorkflowState = DocumentWorkflowState.Approved;
        document.Status = DocumentWorkflowStateMachine.MapToDocumentStatus(document.WorkflowState);
        document.ApproverUserId = _currentUserService.UserId;
        document.ApprovedAt = _dateTimeProvider.UtcNow;
        document.Comments = request.Comments;
        document.ModifiedAt = _dateTimeProvider.UtcNow;
        document.ModifiedBy = _currentUserService.UserId;
        document.RejectedReason = null; // Clear any previous rejection reason

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "ApproveDocument",
            "Document",
            document.Id,
            new
            {
                PreviousState = previousState.ToString(),
                NewState = document.WorkflowState.ToString(),
                ApprovedBy = _currentUserService.UserId,
                ApprovedAt = document.ApprovedAt,
                Comments = request.Comments
            },
            cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Document.Approved",
            "Document",
            document.Id.ToString(),
            new
            {
                PreviousState = previousState.ToString(),
                NewState = document.WorkflowState.ToString(),
                ApprovedBy = _currentUserService.UserId,
                ApprovedAt = document.ApprovedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Comments = request.Comments,
                Title = document.Title
            },
            cancellationToken);
    }
}

