using Maemo.Application.Common;
using Maemo.Application.Documents.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Documents.Queries;

/// <summary>
/// Handler for getting all versions of a document.
/// </summary>
public class GetDocumentVersionsQueryHandler : IRequestHandler<GetDocumentVersionsQuery, IReadOnlyList<DocumentVersionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;

    public GetDocumentVersionsQueryHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider)
    {
        _context = context;
        _tenantProvider = tenantProvider;
    }

    public async Task<IReadOnlyList<DocumentVersionDto>> Handle(GetDocumentVersionsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Verify document exists and belongs to tenant
        var document = await _context.Documents
            .Where(d => d.Id == request.DocumentId && d.TenantId == tenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document == null)
        {
            throw new InvalidOperationException($"Document with ID {request.DocumentId} not found.");
        }

        // Get all versions for this document
        var versions = await _context.DocumentVersions
            .Where(dv => dv.DocumentId == request.DocumentId)
            .OrderByDescending(dv => dv.VersionNumber)
            .Select(dv => new DocumentVersionDto
            {
                Id = dv.Id,
                DocumentId = dv.DocumentId,
                VersionNumber = dv.VersionNumber,
                FileName = dv.FileName,
                StorageLocation = dv.StorageLocation,
                UploadedBy = dv.UploadedBy,
                UploadedAt = dv.UploadedAt,
                Comment = dv.Comment,
                IsLatest = dv.IsLatest
            })
            .ToListAsync(cancellationToken);

        return versions;
    }
}

