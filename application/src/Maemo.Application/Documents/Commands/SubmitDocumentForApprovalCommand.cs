using MediatR;

namespace Maemo.Application.Documents.Commands;

public class SubmitDocumentForApprovalCommand : IRequest
{
    public Guid DocumentId { get; set; }
}

