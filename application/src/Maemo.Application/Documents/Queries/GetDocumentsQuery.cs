using Maemo.Application.Documents.Dtos;
using Maemo.Domain.Documents;
using MediatR;

namespace Maemo.Application.Documents.Queries;

public class GetDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>
{
    public DocumentStatus? Status { get; set; }
    public string? Department { get; set; }
    public string? Category { get; set; }
    public bool IncludeAllVersions { get; set; } = false;
}

