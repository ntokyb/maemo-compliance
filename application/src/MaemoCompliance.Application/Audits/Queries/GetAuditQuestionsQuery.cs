using MaemoCompliance.Application.Audits.Dtos;
using MediatR;

namespace MaemoCompliance.Application.Audits.Queries;

public class GetAuditQuestionsQuery : IRequest<IReadOnlyList<AuditQuestionDto>>
{
    public Guid AuditTemplateId { get; set; }
}

