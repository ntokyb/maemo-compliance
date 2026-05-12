using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class SubmitDocumentForApprovalCommand : IRequest
{
    public Guid DocumentId { get; set; }
}

