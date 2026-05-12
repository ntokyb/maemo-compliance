using MediatR;

namespace Maemo.Application.Documents.Commands;

/// <summary>
/// Command to delete a document and its associated file.
/// </summary>
public class DeleteDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
}

