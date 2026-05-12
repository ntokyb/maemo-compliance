using MediatR;
using MaemoCompliance.Application.Documents.Dtos;

namespace MaemoCompliance.Application.Documents.Queries;

public class GetDocumentAuditEvidenceQuery : IRequest<AuditEvidenceDto>
{
    public Guid DocumentId { get; set; }
}

