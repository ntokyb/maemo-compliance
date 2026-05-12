using MaemoCompliance.Application.Evidence.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Evidence.Queries;

public class GetEvidenceRegisterQuery : IRequest<IReadOnlyList<EvidenceItemDto>>
{
    public Guid? TenantId { get; set; }
    public string? EntityType { get; set; } // Document, DocumentVersion, AuditAnswer
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Limit { get; set; } = 100;
}

