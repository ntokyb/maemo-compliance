using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class RejectDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
    public string RejectedReason { get; set; } = null!;
}

