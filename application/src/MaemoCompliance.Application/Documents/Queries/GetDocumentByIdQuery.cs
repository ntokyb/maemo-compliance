using MaemoCompliance.Application.Documents.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetDocumentByIdQuery : IRequest<DocumentDto?>
{
    public Guid Id { get; set; }
}

