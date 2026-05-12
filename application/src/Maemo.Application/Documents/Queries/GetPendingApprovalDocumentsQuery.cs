using Maemo.Application.Documents.Dtos;
using MediatR;

namespace Maemo.Application.Documents.Queries;

public class GetPendingApprovalDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>
{
}

