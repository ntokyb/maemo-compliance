using Maemo.Application.Documents.Dtos;
using MediatR;

namespace Maemo.Application.Documents.Commands;

public class UpdateDocumentCommand : IRequest
{
    public Guid Id { get; set; }
    public UpdateDocumentRequest Request { get; set; } = null!;
}

