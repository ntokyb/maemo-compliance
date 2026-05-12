using MediatR;

namespace Maemo.Application.Documents.Commands;

public class ArchiveDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
}
