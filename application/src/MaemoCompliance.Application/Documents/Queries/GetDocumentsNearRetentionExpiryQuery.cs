using MediatR;
using MaemoCompliance.Application.Documents.Dtos;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetDocumentsNearRetentionExpiryQuery : IRequest<IReadOnlyList<DocumentDto>>
{
    public int DaysAhead { get; set; } = 90; // Default to 90 days
}

