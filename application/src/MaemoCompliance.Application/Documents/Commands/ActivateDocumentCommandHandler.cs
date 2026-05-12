using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Commands;

public class ActivateDocumentCommandHandler : IRequestHandler<ActivateDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public ActivateDocumentCommandHandler(
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

    public async Task Handle(ActivateDocumentCommand request, CancellationToken cancellationToken)
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
        if (!DocumentWorkflowStateMachine.CanTransition(document.WorkflowState, DocumentWorkflowState.Active))
        {
            throw new InvalidOperationException(
                $"Document cannot transition from {document.WorkflowState} to Active. Allowed transitions: {string.Join(", ", DocumentWorkflowStateMachine.GetAllowedTransitions(document.WorkflowState))}.");
        }

        // Transition to Active
        var previousState = document.WorkflowState;
        document.WorkflowState = DocumentWorkflowState.Active;
        document.Status = DocumentWorkflowStateMachine.MapToDocumentStatus(document.WorkflowState);
        document.ModifiedAt = _dateTimeProvider.UtcNow;
        document.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "ActivateDocument",
            "Document",
            document.Id,
            new
            {
                PreviousState = previousState.ToString(),
                NewState = document.WorkflowState.ToString(),
                ActivatedBy = _currentUserService.UserId
            },
            cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Document.Activated",
            "Document",
            document.Id.ToString(),
            new
            {
                PreviousState = previousState.ToString(),
                NewState = document.WorkflowState.ToString(),
                ActivatedBy = _currentUserService.UserId,
                Title = document.Title
            },
            cancellationToken);
    }
}

