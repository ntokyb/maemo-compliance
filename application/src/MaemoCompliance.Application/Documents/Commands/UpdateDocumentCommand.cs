using MaemoCompliance.Application.Documents.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Documents.Commands;

public class UpdateDocumentCommand : IRequest
{
    public Guid Id { get; set; }
    public UpdateDocumentRequest Request { get; set; } = null!;
}

