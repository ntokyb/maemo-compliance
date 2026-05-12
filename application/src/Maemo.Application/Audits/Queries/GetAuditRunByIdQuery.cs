using Maemo.Application.Audits.Dtos;
using MediatR;

namespace Maemo.Application.Audits.Queries;

public class GetAuditRunByIdQuery : IRequest<AuditRunDto>
{
    public Guid Id { get; set; }
}
