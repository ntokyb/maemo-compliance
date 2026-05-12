using Maemo.Application.Documents.Dtos;
using MediatR;

namespace Maemo.Application.Documents.Commands;

public class CreateDocumentCommand : IRequest<Guid>
{
    public CreateDocumentRequest Request { get; set; } = null!;
}

