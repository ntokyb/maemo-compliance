using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Commands;

public class ChangeDocumentStatusCommandHandler : IRequestHandler<ChangeDocumentStatusCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogger _auditLogger;

    public ChangeDocumentStatusCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IAuditLogger auditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _auditLogger = auditLogger;
    }

    public async Task Handle(ChangeDocumentStatusCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var document = await _context.Documents
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {request.DocumentId} not found.");
        }

        var previousStatus = document.Status;
        var previousWorkflowState = document.WorkflowState;
        
        document.Status = request.NewStatus;
        document.ModifiedAt = _dateTimeProvider.UtcNow;
        document.ModifiedBy = _currentUserService.UserId;

        // Sync workflow state with status changes (for backward compatibility)
        // Note: Direct status changes should ideally go through workflow commands
        switch (request.NewStatus)
        {
            case DocumentStatus.Draft:
                // Only allow transition to Draft if currently in PendingApproval (rejection scenario)
                if (document.WorkflowState == DocumentWorkflowState.PendingApproval)
                {
                    document.WorkflowState = DocumentWorkflowState.Draft;
                }
                break;
            case DocumentStatus.UnderReview:
                // Status UnderReview maps to PendingApproval workflow state
                if (document.WorkflowState == DocumentWorkflowState.Draft)
                {
                    document.WorkflowState = DocumentWorkflowState.PendingApproval;
                }
                break;
            case DocumentStatus.Approved:
                if (document.WorkflowState == DocumentWorkflowState.PendingApproval)
                {
                    document.WorkflowState = DocumentWorkflowState.Approved;
                }
                break;
            case DocumentStatus.Active:
                // Status Active maps to Active workflow state
                // Only allow if document is Approved or already Active
                if (document.WorkflowState == DocumentWorkflowState.Approved || 
                    document.WorkflowState == DocumentWorkflowState.Active)
                {
                    document.WorkflowState = DocumentWorkflowState.Active;
                    document.ApprovedAt = _dateTimeProvider.UtcNow;
                    if (!string.IsNullOrWhiteSpace(request.ApproverUserId))
                    {
                        document.ApproverUserId = request.ApproverUserId;
                    }
                    else if (!string.IsNullOrWhiteSpace(_currentUserService.UserId))
                    {
                        // Use current user if approver not specified
                        document.ApproverUserId = _currentUserService.UserId;
                    }
                }
                break;
            case DocumentStatus.Archived:
                // Status Archived maps to Archived workflow state
                if (document.WorkflowState == DocumentWorkflowState.Active)
                {
                    document.WorkflowState = DocumentWorkflowState.Archived;
                }
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Audit log
        await _auditLogger.LogAsync(
            "ChangeDocumentStatus",
            "Document",
            document.Id,
            new 
            { 
                PreviousStatus = previousStatus.ToString(), 
                NewStatus = request.NewStatus.ToString(),
                PreviousWorkflowState = previousWorkflowState.ToString(),
                NewWorkflowState = document.WorkflowState.ToString(),
                ApproverUserId = document.ApproverUserId 
            },
            cancellationToken);
    }
}

