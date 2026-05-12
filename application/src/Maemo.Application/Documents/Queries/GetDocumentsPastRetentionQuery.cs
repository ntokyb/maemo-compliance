using MediatR;
using Maemo.Application.Documents.Dtos;

namespace Maemo.Application.Documents.Queries;

public class GetDocumentsPastRetentionQuery : IRequest<IReadOnlyList<DocumentDto>>
{
}

