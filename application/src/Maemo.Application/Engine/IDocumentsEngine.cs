using Maemo.Application.Documents.Dtos;
using Maemo.Domain.Documents;

namespace Maemo.Application.Engine;

/// <summary>
/// Engine interface for document management operations.
/// Provides a stable API surface for the Maemo Compliance Engine.
/// </summary>
public interface IDocumentsEngine
{
    /// <summary>
    /// Creates a new document.
    /// </summary>
    Task<Guid> CreateAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    Task UpdateAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists documents based on filter criteria.
    /// </summary>
    Task<IReadOnlyList<DocumentDto>> ListAsync(DocumentFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    Task<DocumentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an existing document.
    /// </summary>
    Task<Guid> CreateNewVersionAsync(Guid id, CreateNewDocumentVersionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of a document.
    /// </summary>
    Task ChangeStatusAsync(Guid id, DocumentStatus newStatus, string? approverUserId, CancellationToken cancellationToken = default);
}

