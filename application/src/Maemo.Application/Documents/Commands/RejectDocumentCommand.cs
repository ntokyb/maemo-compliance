using MediatR;

namespace Maemo.Application.Documents.Commands;

public class RejectDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
    public string RejectedReason { get; set; } = null!;
}

