using Maemo.Application.Documents.Dtos;
using MediatR;

namespace Maemo.Application.Documents.Commands;

public class CreateNewDocumentVersionCommand : IRequest<Guid>
{
    public Guid ExistingDocumentId { get; set; }
    public CreateNewDocumentVersionRequest Request { get; set; } = null!;
}

