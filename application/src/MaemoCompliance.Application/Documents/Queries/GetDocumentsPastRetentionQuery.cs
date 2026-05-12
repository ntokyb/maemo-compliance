using MediatR;
using MaemoCompliance.Application.Documents.Dtos;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetDocumentsPastRetentionQuery : IRequest<IReadOnlyList<DocumentDto>>
{
}

