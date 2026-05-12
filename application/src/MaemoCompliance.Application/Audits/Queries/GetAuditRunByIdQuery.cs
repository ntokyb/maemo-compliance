using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditRunByIdQuery : IRequest<AuditRunDto>
{
    public Guid Id { get; set; }
}
