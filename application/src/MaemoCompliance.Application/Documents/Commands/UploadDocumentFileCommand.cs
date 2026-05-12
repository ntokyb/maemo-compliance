using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class UploadDocumentFileCommand : IRequest<string>
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = null!;
    public Stream FileContent { get; set; } = null!;
}

