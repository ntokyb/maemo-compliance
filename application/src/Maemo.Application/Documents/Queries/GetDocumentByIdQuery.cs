using Maemo.Application.Documents.Dtos;
using MediatR;

namespace Maemo.Application.Documents.Queries;

public class GetDocumentByIdQuery : IRequest<DocumentDto?>
{
    public Guid Id { get; set; }
}

