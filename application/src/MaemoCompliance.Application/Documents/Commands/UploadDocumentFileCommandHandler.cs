using MaemoCompliance.Application.Common;
using MaemoCompliance.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace MaemoCompliance.Application.Documents.Commands;

public class UploadDocumentFileCommandHandler : IRequestHandler<UploadDocumentFileCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IFileHashService _fileHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UploadDocumentFileCommandHandler> _logger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public UploadDocumentFileCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IFileStorageProvider fileStorageProvider,
        IFileHashService fileHashService,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        ILogger<UploadDocumentFileCommandHandler> logger,
        IBusinessAuditLogger businessAuditLogger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _fileStorageProvider = fileStorageProvider;
        _fileHashService = fileHashService;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
        _logger = logger;
        _businessAuditLogger = businessAuditLogger;
    }

    public async Task<string> Handle(UploadDocumentFileCommand command, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Load document and verify it belongs to current tenant
        var document = await _context.Documents
            .Include(d => d.Versions)
            .Where(d => d.Id == command.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new KeyNotFoundException($"Document with Id {command.DocumentId} not found.");
        }

        // Check if document already has versions
        var hasVersions = await _context.DocumentVersions
            .AnyAsync(dv => dv.DocumentId == command.DocumentId, cancellationToken);

        if (hasVersions)
        {
            // If versions exist, this should use CreateDocumentVersionCommand instead
            // For backward compatibility, we'll create a new version
            _logger.LogWarning(
                "UploadDocumentFileCommand called for document {DocumentId} that already has versions. " +
                "Consider using CreateDocumentVersionCommand instead.",
                command.DocumentId);
        }

        // Compute file hash for integrity verification
        // Create a copy of the stream for hashing (since SaveAsync will consume the stream)
        string? fileHash = null;
        Stream hashStream = command.FileContent;
        if (!command.FileContent.CanSeek)
        {
            // If stream is not seekable, copy to MemoryStream
            var memoryStream = new MemoryStream();
            await command.FileContent.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            hashStream = memoryStream;
        }
        else
        {
            // Save current position
            var originalPosition = command.FileContent.Position;
            command.FileContent.Position = 0;
        }

        fileHash = await _fileHashService.ComputeSha256HashAsync(hashStream, cancellationToken);

        // Reset stream position for upload
        if (command.FileContent.CanSeek)
        {
            command.FileContent.Position = 0;
        }
        else if (hashStream is MemoryStream ms)
        {
            ms.Position = 0;
        }

        // Use file storage provider (automatically selects local or SharePoint based on deployment mode)
        var category = document.Category ?? "General";
        var uploadStream = hashStream is MemoryStream ? hashStream : command.FileContent;
        var storageLocation = await _fileStorageProvider.SaveAsync(
            tenantId,
            uploadStream,
            command.FileName,
            category,
            cancellationToken);

        // If no versions exist, create version 1
        if (!hasVersions)
        {
            var version1 = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                VersionNumber = 1,
                FileName = command.FileName,
                StorageLocation = storageLocation,
                FileHash = fileHash,
                UploadedBy = _currentUserService.UserId,
                UploadedAt = _dateTimeProvider.UtcNow,
                IsLatest = true,
                CreatedAt = _dateTimeProvider.UtcNow,
                CreatedBy = _currentUserService.UserId
            };

            _context.DocumentVersions.Add(version1);
            document.Version = 1;
            document.FileHash = fileHash;
        }

        // Update document with storage location and hash
        document.StorageLocation = storageLocation;
        if (!hasVersions)
        {
            document.FileHash = fileHash;
        }
        await _context.SaveChangesAsync(cancellationToken);

        // Log business audit event
        await _businessAuditLogger.LogAsync(
            "Document.FileUploaded",
            "Document",
            document.Id.ToString(),
            new { FileName = command.FileName, StorageLocation = storageLocation, VersionNumber = document.Version },
            cancellationToken);

        return storageLocation;
    }
}

