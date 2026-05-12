using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class ArchiveDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
}
