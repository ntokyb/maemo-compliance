using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Queries;

public sealed class GetAuditProgrammeByYearQuery : IRequest<AuditProgrammeDto?>
{
    public int Year { get; set; }
}
