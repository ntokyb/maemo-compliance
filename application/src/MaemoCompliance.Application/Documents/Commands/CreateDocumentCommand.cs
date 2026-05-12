using MaemoCompliance.Application.Documents.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class CreateDocumentCommand : IRequest<Guid>
{
    public CreateDocumentRequest Request { get; set; } = null!;
}

