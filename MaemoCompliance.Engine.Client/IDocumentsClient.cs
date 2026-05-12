using MaemoCompliance.Engine.Client.Models;

namespace MaemoCompliance.Engine.Client;

/// <summary>
/// Client interface for document management operations in the Maemo Compliance Engine.
/// </summary>
public interface IDocumentsClient
{
    /// <summary>
    /// Retrieves a list of documents for the current tenant, optionally filtered by status and department.
    /// </summary>
    /// <param name="status">Optional document status filter.</param>
    /// <param name="department">Optional department filter.</param>
    /// <param name="includeAllVersions">Whether to include all document versions or only current versions.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of documents.</returns>
    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(
        DocumentStatus? status = null,
        string? department = null,
        bool includeAllVersions = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific document by its unique identifier.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The document, or null if not found.</returns>
    Task<DocumentDto?> GetDocumentAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new compliance document.
    /// </summary>
    /// <param name="request">The document creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the created document.</returns>
    Task<Guid> CreateDocumentAsync(CreateDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="request">The document update request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UpdateDocumentAsync(Guid id, UpdateDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an existing document.
    /// </summary>
    /// <param name="id">The existing document ID.</param>
    /// <param name="request">The new version request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The ID of the new document version.</returns>
    Task<Guid> CreateDocumentVersionAsync(Guid id, CreateNewDocumentVersionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the status of a document (e.g., Draft → Active → Archived).
    /// </summary>
    /// <param name="id">The document ID.</param>
    /// <param name="status">The new status.</param>
    /// <param name="approverUserId">Optional approver user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ChangeDocumentStatusAsync(Guid id, DocumentStatus status, string? approverUserId = null, CancellationToken cancellationToken = default);
}
