using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class ActivateDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
}

