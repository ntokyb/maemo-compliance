using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

/// <summary>
/// Command to create a new version of an existing document.
/// </summary>
public class CreateDocumentVersionCommand : IRequest<Guid>
{
    public Guid DocumentId { get; set; }
    public Stream FileContent { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string? Comment { get; set; }
}

