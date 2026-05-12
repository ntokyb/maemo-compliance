using MediatR;
using MaemoCompliance.Application.Documents.Dtos;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetPopiaPersonalInfoDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>
{
}

