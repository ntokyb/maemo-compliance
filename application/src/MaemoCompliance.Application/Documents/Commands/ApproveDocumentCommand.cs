using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class ApproveDocumentCommand : IRequest
{
    public Guid DocumentId { get; set; }
    public string? Comments { get; set; }
    public string? ApproverName { get; set; }
}

