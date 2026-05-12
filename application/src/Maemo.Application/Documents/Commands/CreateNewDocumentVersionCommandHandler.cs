using Maemo.Application.Common;
using Maemo.Application.Documents.Dtos;
using Maemo.Domain.Documents;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Maemo.Application.Documents.Commands;

public class CreateNewDocumentVersionCommandHandler : IRequestHandler<CreateNewDocumentVersionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public CreateNewDocumentVersionCommandHandler(
        IApplicationDbContext context,
        ITenantProvider tenantProvider,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(CreateNewDocumentVersionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();

        // Find the existing current document
        var existingDocument = await _context.Documents
            .Where(d => d.Id == request.ExistingDocumentId && d.TenantId == tenantId && d.IsCurrentVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingDocument == null)
        {
            throw new InvalidOperationException($"Document with ID {request.ExistingDocumentId} not found or is not the current version.");
        }

        // Set existing document's IsCurrentVersion to false
        existingDocument.IsCurrentVersion = false;
        existingDocument.ModifiedAt = _dateTimeProvider.UtcNow;
        existingDocument.ModifiedBy = _currentUserService.UserId;

        // Create new version
        var newVersion = new Document
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = request.Request.Title,
            Category = request.Request.Category,
            Department = request.Request.Department,
            OwnerUserId = request.Request.OwnerUserId,
            ReviewDate = request.Request.ReviewDate,
            Status = DocumentStatus.Draft,
            Version = existingDocument.Version + 1,
            PreviousVersionId = existingDocument.Id,
            IsCurrentVersion = true,
            Comments = request.Request.Comments,
            CreatedAt = _dateTimeProvider.UtcNow,
            CreatedBy = _currentUserService.UserId
        };

        _context.Documents.Add(newVersion);
        await _context.SaveChangesAsync(cancellationToken);

        return newVersion.Id;
    }
}

