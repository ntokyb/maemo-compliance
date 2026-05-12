using MaemoCompliance.Application.Documents.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetPendingApprovalDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>
{
}

