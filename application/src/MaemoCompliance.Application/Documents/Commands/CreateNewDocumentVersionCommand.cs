using MaemoCompliance.Application.Documents.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class CreateNewDocumentVersionCommand : IRequest<Guid>
{
    public Guid ExistingDocumentId { get; set; }
    public CreateNewDocumentVersionRequest Request { get; set; } = null!;
}

