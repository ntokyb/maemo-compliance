using MediatR;

namespace Maemo.Application.Documents.Commands;

public class ApproveDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
    public string? Comments { get; set; }
}

