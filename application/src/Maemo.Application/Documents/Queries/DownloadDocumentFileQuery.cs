using MediatR;

namespace Maemo.Application.Documents.Queries;

/// <summary>
/// Query to download a document file.
/// </summary>
public class DownloadDocumentFileQuery : IRequest<Stream?>
{
    public Guid DocumentId { get; set; }
}

