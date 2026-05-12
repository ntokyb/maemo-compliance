using MediatR;
using Maemo.Application.Documents.Dtos;

namespace Maemo.Application.Documents.Queries;

public class GetPopiaPersonalInfoDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>
{
}

