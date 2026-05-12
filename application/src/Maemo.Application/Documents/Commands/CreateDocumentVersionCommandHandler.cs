using Maemo.Application.Common;
using Maemo.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Maemo.Application.Documents.Commands;

/// <summary>
/// Handler for creating a new document version.
/// </summary>
public class CreateDocumentVersionCommandHandler : IRequestHandler<CreateDocumentVersionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IFileHashService _fileHashService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateDocumentVersionCommandHandler> _logger;
    private readonly IBusinessAuditLogger _businessAuditLogger;

    public CreateDocumentVersionCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IFileStorageProvider fileStorageProvider,
        IFileHashService fileHashService,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService,
        ILogger<CreateDocumentVersionCommandHandler> logger,
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

    public async Task<Guid> Handle(CreateDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Load document and verify it belongs to current tenant
        var document = await _context.Documents
            .Include(d => d.Versions)
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {request.DocumentId} not found.");
        }

        // Get the latest version number
        var latestVersion = await _context.DocumentVersions
            .Where(dv => dv.DocumentId == request.DocumentId)
            .OrderByDescending(dv => dv.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var nextVersionNumber = latestVersion == null ? 1 : latestVersion.VersionNumber + 1;

        // Mark previous latest version as not latest
        if (latestVersion != null)
        {
            latestVersion.IsLatest = false;
        }

        // Compute file hash for integrity verification
        // Create a copy of the stream for hashing (since SaveAsync will consume the stream)
        string? fileHash = null;
        Stream hashStream = request.FileContent;
        if (!request.FileContent.CanSeek)
        {
            // If stream is not seekable, copy to MemoryStream
            var memoryStream = new MemoryStream();
            await request.FileContent.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            hashStream = memoryStream;
        }
        else
        {
            // Save current position
            var originalPosition = request.FileContent.Position;
            request.FileContent.Position = 0;
        }

        fileHash = await _fileHashService.ComputeSha256HashAsync(hashStream, cancellationToken);

        // Reset stream position for upload
        if (request.FileContent.CanSeek)
        {
            request.FileContent.Position = 0;
        }
        else if (hashStream is MemoryStream ms)
        {
            ms.Position = 0;
        }

        // Upload file to storage with version-specific category path
        // The storage provider will use: category/documents/{documentId}/v{versionNumber}/{fileName}
        var category = $"documents/{request.DocumentId}/v{nextVersionNumber}";
        
        var uploadStream = hashStream is MemoryStream ? hashStream : request.FileContent;
        var storageLocation = await _fileStorageProvider.SaveAsync(
            tenantId,
            uploadStream,
            request.FileName,
            category,
            cancellationToken);

        // Create new DocumentVersion entity
        var newVersion = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = request.DocumentId,
            VersionNumber = nextVersionNumber,
            FileName = request.FileName,
            StorageLocation = storageLocation,
            FileHash = fileHash,
            UploadedBy = _currentUserService.UserId,
            UploadedAt = _dateTimeProvider.UtcNow,
            Comment = request.Comment,
            IsLatest = true,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.UserId
        };

        _context.DocumentVersions.Add(newVersion);

        // Update document's StorageLocation to point to latest version
        document.StorageLocation = storageLocation;
        document.Version = nextVersionNumber;
        document.ModifiedAt = _dateTimeProvider.UtcNow;
        document.ModifiedBy = _currentUserService.UserId;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created new version {VersionNumber} for document {DocumentId}",
            nextVersionNumber,
            request.DocumentId);

        // Log business audit event
        await _businessAuditLogger.LogAsync(
            "Document.VersionCreated",
            "Document",
            request.DocumentId.ToString(),
            new { VersionNumber = nextVersionNumber, FileName = request.FileName, Comment = request.Comment },
            cancellationToken);

        return newVersion.Id;
    }
}

