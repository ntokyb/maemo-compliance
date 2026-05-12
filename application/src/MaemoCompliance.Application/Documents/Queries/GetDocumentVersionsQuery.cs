using MaemoCompliance.Application.Documents.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Documents.Queries;

/// <summary>
/// Query to get all versions of a document.
/// </summary>
public class GetDocumentVersionsQuery : IRequest<IReadOnlyList<DocumentVersionDto>>
{
    public Guid DocumentId { get; set; }
}

