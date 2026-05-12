using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MaemoCompliance.Application.Documents.Commands;

public class ArchiveDocumentCommandHandler : IRequestHandler<ArchiveDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public ArchiveDocumentCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(ArchiveDocumentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == request.DocumentId && d.TenantId == tenantId, cancellationToken);

        if (document == null)
        {
            throw new KeyNotFoundException($"Document with ID {request.DocumentId} was not found.");
        }

        // Check if document can be archived (state machine: Active → Archived)
        if (!DocumentWorkflowStateMachine.CanTransition(document.WorkflowState, DocumentWorkflowState.Archived))
        {
            throw new ConflictException(
                $"Document cannot be archived from {document.WorkflowState} state. Only an active document can be archived.");
        }

        var oldWorkflowState = document.WorkflowState;

        // Archive the document
        document.WorkflowState = DocumentWorkflowState.Archived;
        document.Status = DocumentStatus.Archived;
        document.IsPendingArchive = false; // Clear pending flag
        document.ModifiedAt = _dateTimeProvider.UtcNow;
        document.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        // Business audit log
        await _businessAuditLogger.LogAsync(
            "Document.Archived",
            "Document",
            document.Id.ToString(),
            new 
            { 
                Title = document.Title,
                OldWorkflowState = oldWorkflowState.ToString(),
                NewWorkflowState = document.WorkflowState.ToString(),
                RetainUntil = document.RetainUntil?.ToString("yyyy-MM-dd"),
                ArchivedBy = _currentUserService.UserId,
                ArchivedAt = _dateTimeProvider.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
            },
            cancellationToken);
    }
}
