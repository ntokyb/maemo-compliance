using Maemo.Application.Common;
using Maemo.Application.Documents.Commands;
using Maemo.Application.Documents.Dtos;
using Maemo.Application.Documents.Queries;
using Maemo.Application.Webhooks;
using Maemo.Domain.Documents;
using MediatR;

namespace Maemo.Application.Engine;

/// <summary>
/// Engine implementation for document management operations.
/// Acts as a facade over MediatR commands and queries.
/// </summary>
public class DocumentsEngine : IDocumentsEngine
{
    private readonly IMediator _mediator;
    private readonly IWebhookDispatcher _webhookDispatcher;
    private readonly ITenantProvider _tenantProvider;

    public DocumentsEngine(IMediator mediator, IWebhookDispatcher webhookDispatcher, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _webhookDispatcher = webhookDispatcher;
        _tenantProvider = tenantProvider;
    }

    public async Task<Guid> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateDocumentCommand
        {
            Request = request
        };

        var documentId = await _mediator.Send(command, cancellationToken);

        // Dispatch webhook event
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _ = _webhookDispatcher.EnqueueAsync(tenantId, "Document.Created", new { DocumentId = documentId, Title = request.Title }, cancellationToken);

        return documentId;
    }

    public async Task UpdateAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var command = new UpdateDocumentCommand
        {
            Id = id,
            Request = request
        };

        await _mediator.Send(command, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentDto>> ListAsync(DocumentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = new GetDocumentsQuery
        {
            Status = filter.Status,
            Department = filter.Department,
            IncludeAllVersions = filter.IncludeAllVersions
        };

        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<DocumentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var query = new GetDocumentByIdQuery { Id = id };
        return await _mediator.Send(query, cancellationToken);
    }

    public async Task<Guid> CreateNewVersionAsync(Guid id, CreateNewDocumentVersionRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateNewDocumentVersionCommand
        {
            ExistingDocumentId = id,
            Request = request
        };

        var newVersionId = await _mediator.Send(command, cancellationToken);

        // Dispatch webhook event
        var tenantId = _tenantProvider.GetCurrentTenantId();
        _ = _webhookDispatcher.EnqueueAsync(tenantId, "Document.VersionCreated", new { DocumentId = id, NewVersionId = newVersionId, Title = request.Title }, cancellationToken);

        return newVersionId;
    }

    public async Task ChangeStatusAsync(Guid id, DocumentStatus newStatus, string? approverUserId, CancellationToken cancellationToken = default)
    {
        var command = new ChangeDocumentStatusCommand
        {
            DocumentId = id,
            NewStatus = newStatus,
            ApproverUserId = approverUserId
        };

        await _mediator.Send(command, cancellationToken);
    }
}

