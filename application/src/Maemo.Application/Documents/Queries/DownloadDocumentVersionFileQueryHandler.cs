using Maemo.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Documents.Queries;

/// <summary>
/// Handler for downloading a specific document version file.
/// </summary>
public class DownloadDocumentVersionFileQueryHandler : IRequestHandler<DownloadDocumentVersionFileQuery, Stream?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IBusinessAuditLogger _businessAuditLogger;
    private readonly ICurrentUserService _currentUserService;

    public DownloadDocumentVersionFileQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IFileStorageProvider fileStorageProvider,
        IBusinessAuditLogger businessAuditLogger,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _fileStorageProvider = fileStorageProvider;
        _businessAuditLogger = businessAuditLogger;
        _currentUserService = currentUserService;
    }

    public async Task<Stream?> Handle(DownloadDocumentVersionFileQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify document exists and belongs to tenant
        var document = await _context.Documents
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            return null;
        }

        // Load the specific version
        var version = await _context.DocumentVersions
            .Where(dv => dv.DocumentId == request.DocumentId && dv.VersionNumber == request.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            return null;
        }

        // Log document access for POPIA compliance
        await _businessAuditLogger.LogAsync(
            "Document.Accessed",
            "Document",
            request.DocumentId.ToString(),
            new 
            { 
                Title = document.Title,
                PiiDataType = document.PiiDataType.ToString(),
                PersonalInformationType = document.PersonalInformationType.ToString(),
                VersionNumber = request.VersionNumber,
                AccessedBy = _currentUserService.UserId
            },
            cancellationToken);

        // Download file from storage provider
        return await _fileStorageProvider.GetAsync(tenantId, version.StorageLocation, cancellationToken);
    }
}

