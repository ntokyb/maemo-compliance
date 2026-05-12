using Maemo.Application.Common;
using Maemo.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Maemo.Application.Documents.Commands;

/// <summary>
/// Handler for destroying documents according to retention rules.
/// Performs soft destruction (marks as destroyed) rather than hard deletion.
/// </summary>
public class DestroyDocumentCommandHandler : IRequestHandler<DestroyDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBusinessAuditLogger _businessAuditLogger;
    private readonly ILogger<DestroyDocumentCommandHandler> _logger;

    public DestroyDocumentCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        IBusinessAuditLogger businessAuditLogger,
        ILogger<DestroyDocumentCommandHandler> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _businessAuditLogger = businessAuditLogger;
        _logger = logger;
    }

    public async Task Handle(DestroyDocumentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var now = _dateTimeProvider.UtcNow;

        // Load document - use IgnoreQueryFilters to include destroyed documents for validation
        var document = await _context.Documents
            .IgnoreQueryFilters()
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with Id {request.DocumentId} not found.");
        }

        // Validation: Check if already destroyed
        if (document.IsDestroyed)
        {
            throw new InvalidOperationException($"Document with Id {request.DocumentId} has already been destroyed on {document.DestroyedAt:yyyy-MM-dd HH:mm:ss} by {document.DestroyedByUserId}.");
        }

        // Validation: Check retention lock
        if (document.IsRetentionLocked)
        {
            throw new InvalidOperationException(
                $"Document with Id {request.DocumentId} is retention-locked and cannot be destroyed. " +
                "Unlock retention first if destruction is required.");
        }

        // Validation: Check retention date
        if (document.RetainUntil.HasValue && document.RetainUntil.Value > now)
        {
            throw new InvalidOperationException(
                $"Document with Id {request.DocumentId} cannot be destroyed until {document.RetainUntil.Value:yyyy-MM-dd}. " +
                "Retention period has not expired.");
        }

        // Validation: Check document status (optional rule - don't destroy Draft or PendingApproval)
        if (document.WorkflowState == DocumentWorkflowState.Draft || 
            document.WorkflowState == DocumentWorkflowState.PendingApproval)
        {
            throw new InvalidOperationException(
                $"Document with Id {request.DocumentId} is in {document.WorkflowState} state and cannot be destroyed. " +
                "Complete the approval workflow first.");
        }

        // Collect file paths for audit logging
        var filePaths = new List<string>();
        if (!string.IsNullOrWhiteSpace(document.StorageLocation))
        {
            filePaths.Add(document.StorageLocation);
        }

        var versions = await _context.DocumentVersions
            .Where(dv => dv.DocumentId == request.DocumentId)
            .ToListAsync(cancellationToken);

        foreach (var version in versions)
        {
            if (!string.IsNullOrWhiteSpace(version.StorageLocation) && 
                !filePaths.Contains(version.StorageLocation))
            {
                filePaths.Add(version.StorageLocation);
            }
        }

        // Mark document as destroyed (soft delete)
        document.IsDestroyed = true;
        document.DestroyedAt = now;
        document.DestroyedByUserId = _currentUserService.UserId;
        document.DestroyReason = request.Reason;
        document.ModifiedAt = now;
        document.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Document {DocumentId} ({Title}) has been destroyed by {UserId}. Reason: {Reason}",
            request.DocumentId,
            document.Title,
            _currentUserService.UserId,
            request.Reason);

        // Log business audit event
        await _businessAuditLogger.LogAsync(
            "Document.Destroyed",
            "Document",
            request.DocumentId.ToString(),
            new
            {
                Title = document.Title,
                Reason = request.Reason,
                DestroyedAt = now.ToString("yyyy-MM-dd HH:mm:ss"),
                DestroyedBy = _currentUserService.UserId,
                RetainUntil = document.RetainUntil?.ToString("yyyy-MM-dd"),
                FilePaths = filePaths,
                VersionCount = versions.Count
            },
            cancellationToken);
    }
}

