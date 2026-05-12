using MaemoCompliance.Domain.Documents;
using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class ChangeDocumentStatusCommand : IRequest
{
    public Guid DocumentId { get; set; }
    public DocumentStatus NewStatus { get; set; }
    public string? ApproverUserId { get; set; }
}

