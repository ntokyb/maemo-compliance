using MediatR;

namespace Maemo.Application.Documents.Commands;

public class ActivateDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
}

