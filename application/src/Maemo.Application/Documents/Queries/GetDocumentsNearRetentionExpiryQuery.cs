using MediatR;
using Maemo.Application.Documents.Dtos;

namespace Maemo.Application.Documents.Queries;

public class GetDocumentsNearRetentionExpiryQuery : IRequest<IReadOnlyList<DocumentDto>>
{
    public int DaysAhead { get; set; } = 90; // Default to 90 days
}

