using MediatR;

namespace Maemo.Application.Documents.Queries;

/// <summary>
/// Query to download a specific document version file.
/// </summary>
public class DownloadDocumentVersionFileQuery : IRequest<Stream?>
{
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
}

