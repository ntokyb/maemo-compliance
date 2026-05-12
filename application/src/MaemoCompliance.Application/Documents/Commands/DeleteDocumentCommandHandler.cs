using MaemoCompliance.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MaemoCompliance.Application.Documents.Commands;

/// <summary>
/// Handler for deleting documents and their associated files.
/// </summary>
public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly ILogger<DeleteDocumentCommandHandler> _logger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public DeleteDocumentCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IFileStorageProvider fileStorageProvider,
        ILogger<DeleteDocumentCommandHandler> logger,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _fileStorageProvider = fileStorageProvider;
        _logger = logger;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Load document and verify it belongs to current tenant
        var document = await _context.Documents
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with Id {request.DocumentId} not found.");
        }

        // Delete all version files
        var versions = await _context.DocumentVersions
            .Where(dv => dv.DocumentId == request.DocumentId)
            .ToListAsync(cancellationToken);

        foreach (var version in versions)
        {
            if (!string.IsNullOrWhiteSpace(version.StorageLocation))
            {
                try
                {
                    await _fileStorageProvider.DeleteAsync(tenantId, version.StorageLocation, cancellationToken);
                    _logger.LogInformation(
                        "Successfully deleted version {VersionNumber} file for document {DocumentId} - StorageLocation: {StorageLocation}",
                        version.VersionNumber,
                        request.DocumentId,
                        version.StorageLocation);
                }
                catch (Exception ex)
                {
                    // Log error but continue with deletion
                    _logger.LogWarning(
                        ex,
                        "Failed to delete version {VersionNumber} file for document {DocumentId} - StorageLocation: {StorageLocation}. Continuing with deletion.",
                        version.VersionNumber,
                        request.DocumentId,
                        version.StorageLocation);
                }
            }
        }

        // Delete the document's main file if it exists and is different from versions
        if (!string.IsNullOrWhiteSpace(document.StorageLocation) && 
            !versions.Any(v => v.StorageLocation == document.StorageLocation))
        {
            try
            {
                await _fileStorageProvider.DeleteAsync(tenantId, document.StorageLocation, cancellationToken);
                _logger.LogInformation(
                    "Successfully deleted main file for document {DocumentId} - StorageLocation: {StorageLocation}",
                    request.DocumentId,
                    document.StorageLocation);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to delete main file for document {DocumentId} - StorageLocation: {StorageLocation}. Continuing with document deletion.",
                    request.DocumentId,
                    document.StorageLocation);
            }
        }

        // Delete all document versions from database
        _context.DocumentVersions.RemoveRange(versions);

        // Log business audit event before deletion
        await _businessAuditLogger.LogAsync(
            "Document.Deleted",
            "Document",
            request.DocumentId.ToString(),
            new { Title = document.Title, VersionCount = versions.Count },
            cancellationToken);

        // Delete document from database
        _context.Documents.Remove(document);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

