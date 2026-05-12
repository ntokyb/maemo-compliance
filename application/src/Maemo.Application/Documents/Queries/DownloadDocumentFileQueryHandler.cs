using Maemo.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Documents.Queries;

/// <summary>
/// Handler for downloading document files.
/// </summary>
public class DownloadDocumentFileQueryHandler : IRequestHandler<DownloadDocumentFileQuery, Stream?>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IFileStorageProvider _fileStorageProvider;
    private readonly IBusinessAuditLogger _businessAuditLogger;
    private readonly ICurrentUserService _currentUserService;

    public DownloadDocumentFileQueryHandler(
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

    public async Task<Stream?> Handle(DownloadDocumentFileQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Load document and verify it belongs to current tenant
        var document = await _context.Documents
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(document.StorageLocation))
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
                AccessedBy = _currentUserService.UserId
            },
            cancellationToken);

        // Download file from storage provider
        return await _fileStorageProvider.GetAsync(tenantId, document.StorageLocation, cancellationToken);
    }
}

