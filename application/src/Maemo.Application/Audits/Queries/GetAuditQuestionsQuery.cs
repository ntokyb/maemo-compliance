using Maemo.Application.Audits.Dtos;
using MediatR;

namespace Maemo.Application.Audits.Queries;

public class GetAuditQuestionsQuery : IRequest<IReadOnlyList<AuditQuestionDto>>
{
    public Guid AuditTemplateId { get; set; }
}

