using MediatR;
using Maemo.Application.Documents.Dtos;

namespace Maemo.Application.Documents.Queries;

public class GetDocumentAuditEvidenceQuery : IRequest<AuditEvidenceDto>
{
    public Guid DocumentId { get; set; }
}

